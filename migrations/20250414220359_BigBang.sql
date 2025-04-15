﻿CREATE TABLE IF NOT EXISTS "__EFMigrationsHistory" (
    "MigrationId" character varying(150) NOT NULL,
    "ProductVersion" character varying(32) NOT NULL,
    CONSTRAINT "PK___EFMigrationsHistory" PRIMARY KEY ("MigrationId")
);

START TRANSACTION;
DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM pg_namespace WHERE nspname = 'user_management') THEN
        CREATE SCHEMA user_management;
    END IF;
END $EF$;

CREATE TABLE user_management."Roles" (
    "Id" integer NOT NULL,
    "Name" text NOT NULL,
    "Description" text NOT NULL,
    CONSTRAINT "PK_Roles" PRIMARY KEY ("Id")
);

CREATE TABLE user_management."Users" (
    "Id" integer GENERATED BY DEFAULT AS IDENTITY,
    "Name" text NOT NULL,
    "Email" text NOT NULL,
    "Password" bytea NOT NULL,
    "Salt" bytea NOT NULL,
    "RoleId" integer NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "Active" boolean NOT NULL,
    CONSTRAINT "PK_Users" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_Users_Roles_RoleId" FOREIGN KEY ("RoleId") REFERENCES user_management."Roles" ("Id") ON DELETE CASCADE
);

CREATE INDEX "IX_Users_RoleId" ON user_management."Users" ("RoleId");

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20250414220359_BigBang', '9.0.4');

COMMIT;

