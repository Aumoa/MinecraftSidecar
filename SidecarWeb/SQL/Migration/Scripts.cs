using SQLMigration;

namespace SidecarWeb.SQL.Migration;

[DatabaseTarget("MinecraftSidecar")]
public partial class Scripts : IScripts
{
    public IEnumerable<IScript> GetScripts()
    {
        yield return new _1__Init();
    }
}
