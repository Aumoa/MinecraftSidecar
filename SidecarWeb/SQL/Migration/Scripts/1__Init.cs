using SQLMigration;

namespace SidecarWeb.SQL.Migration;

public partial class Scripts
{
    private class _1__Init : IScript
    {
        public string Name => "Init";
        public int InstalledRank => 1;

        public string UpSql => @"
CREATE TABLE `account_role` (
    `account_sub` VARCHAR(128) NOT NULL,
    `name` VARCHAR(32) NOT NULL,
    `granted_at` DATETIME NOT NULL,
    `expired_at` DATETIME,
    PRIMARY KEY (`account_sub`, `name`, `expired_at`)
);
";

        public string DownSql => @"
DROP TABLE `account_role`;
";
    }
}
