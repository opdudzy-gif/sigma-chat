using System.Diagnostics;
using System.Reflection;
using System.Text.Json;

namespace SigmaGame;
static class Updater
{
    const string Latest = "https://api.github.com/repos/opdudzy-gif/sigma-chat/releases/latest";
    public static async Task Check(Form owner)
    {
        try
        {
            using var http=new HttpClient();http.DefaultRequestHeaders.UserAgent.ParseAdd("SigmaChat-Updater/5.0");
            using var doc=JsonDocument.Parse(await http.GetStringAsync(Latest));var root=doc.RootElement;
            var tag=(root.GetProperty("tag_name").GetString()??"").TrimStart('v');
            if(!Version.TryParse(tag,out var online)||online<=Assembly.GetExecutingAssembly().GetName().Version)return;
            var asset=root.GetProperty("assets").EnumerateArray().FirstOrDefault(x=>(x.GetProperty("name").GetString()??"").Equals("sigmagame.exe",StringComparison.OrdinalIgnoreCase));
            if(asset.ValueKind==JsonValueKind.Undefined)return;
            if(MessageBox.Show(owner,$"SigmaChat {online} is available. Install it now?","Update available",MessageBoxButtons.YesNo)!=DialogResult.Yes)return;
            var bytes=await http.GetByteArrayAsync(asset.GetProperty("browser_download_url").GetString());var next=Path.Combine(Path.GetTempPath(),"sigmagame-new.exe");await File.WriteAllBytesAsync(next,bytes);
            var current=Environment.ProcessPath!;var script=Path.Combine(Path.GetTempPath(),"sigmachat-update.cmd");
            await File.WriteAllTextAsync(script,$"@echo off\r\ntimeout /t 2 /nobreak >nul\r\ncopy /y \"{next}\" \"{current}\" >nul\r\nstart \"\" \"{current}\"\r\ndel \"%~f0\"\r\n");
            Process.Start(new ProcessStartInfo(script){UseShellExecute=true,WindowStyle=ProcessWindowStyle.Hidden});Application.Exit();
        }
        catch { /* Updates never prevent chat startup. */ }
    }
}
