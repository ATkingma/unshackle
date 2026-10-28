// Project Config.csproj: no Z package reference. Logic.csproj references Config.csproj and Z.EntityFramework.Plus.EFCore,
// and keeps a Z call (Future) that stays Manual, so Plus stays in Logic.
namespace Config;

public class Settings
{
    public string Name { get; set; } = "";
}
