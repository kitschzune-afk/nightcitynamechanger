using System.Diagnostics;
using System.Text;
namespace Ncnc;
public sealed class Toolchain(string executable, Action<string> log)
{
    public string Executable { get; } = Path.GetFullPath(executable);
    public async Task<string> Run(CancellationToken ct, params string[] args)
    {
        if (!File.Exists(Executable)) throw new FileNotFoundException("WolvenKit.CLI.exe not found. Select the bundled toolchain.", Executable);
        var psi = new ProcessStartInfo(Executable) { UseShellExecute = false, CreateNoWindow = true, RedirectStandardOutput = true, RedirectStandardError = true, WorkingDirectory = Path.GetDirectoryName(Executable)! };
        psi.Environment.Remove("DOTNET_ROOT"); psi.Environment.Remove("DOTNET_ROOT_X64");
        var bundledRuntime = Path.Combine(Path.GetDirectoryName(Executable)!, "runtime");
        if (Directory.Exists(bundledRuntime)) { psi.Environment["DOTNET_ROOT"] = bundledRuntime; psi.Environment["DOTNET_ROOT_X64"] = bundledRuntime; }
        foreach (var a in args) psi.ArgumentList.Add(a);
        using var p = new Process { StartInfo = psi };
        log("WolvenKit: " + string.Join(" ", args));
        p.Start();
        var stdout = p.StandardOutput.ReadToEndAsync(ct);
        var stderr = p.StandardError.ReadToEndAsync(ct);
        try { await p.WaitForExitAsync(ct); }
        catch { try { p.Kill(true); } catch { } throw; }
        var result = await stdout + "\n" + await stderr;
        if (p.ExitCode != 0 || result.Contains(": Error") || result.Contains("Unhandled exception", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException($"WolvenKit failed ({p.ExitCode}): {result[^Math.Min(result.Length, 4000)..]}");
        return result;
    }
}
