using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RaceDay.Migrations;

public partial class StoreUserRoleAsString : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "Role_New",
            table: "Users",
            type: "nvarchar(20)",
            maxLength: 20,
            nullable: true);

        migrationBuilder.Sql("""
            UPDATE [Users]
            SET [Role_New] = CASE [Role]
                WHEN 0 THEN N'Organiser'
                WHEN 1 THEN N'Participant'
                ELSE N'Participant'
            END;
            """);

        migrationBuilder.Sql("""
            IF EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = N'CK_Users_Role')
                ALTER TABLE [Users] DROP CONSTRAINT [CK_Users_Role];
            """);

        migrationBuilder.DropColumn(
            name: "Role",
            table: "Users");

        migrationBuilder.RenameColumn(
            name: "Role_New",
            table: "Users",
            newName: "Role");

        migrationBuilder.AlterColumn<string>(
            name: "Role",
            table: "Users",
            type: "nvarchar(20)",
            maxLength: 20,
            nullable: false,
            oldClrType: typeof(string),
            oldType: "nvarchar(20)",
            oldMaxLength: 20,
            oldNullable: true);

        migrationBuilder.AddCheckConstraint(
            name: "CK_Users_Role",
            table: "Users",
            sql: "[Role] IN ('Organiser','Participant')");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropCheckConstraint(
            name: "CK_Users_Role",
            table: "Users");

        migrationBuilder.AddColumn<int>(
            name: "Role_New",
            table: "Users",
            type: "int",
            nullable: true);

        migrationBuilder.Sql("""
            UPDATE [Users]
            SET [Role_New] = CASE [Role]
                WHEN N'Organiser' THEN 0
                WHEN N'Participant' THEN 1
                ELSE 1
            END;
            """);

        migrationBuilder.DropColumn(
            name: "Role",
            table: "Users");

        migrationBuilder.RenameColumn(
            name: "Role_New",
            table: "Users",
            newName: "Role");

        migrationBuilder.AlterColumn<int>(
            name: "Role",
            table: "Users",
            type: "int",
            nullable: false,
            oldClrType: typeof(int),
            oldType: "int",
            oldNullable: true);
    }
}
