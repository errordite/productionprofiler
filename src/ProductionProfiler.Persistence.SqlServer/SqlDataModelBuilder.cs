﻿using System;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Transactions;
using ProductionProfiler.Core.Extensions;

namespace ProductionProfiler.Persistence.Sql
{
    public static class SqlDataModelBuilder
    {
        public static void BuildDataModel(SqlConfiguration configuration)
        {
            string scriptContents = GetScript(configuration);

            if(configuration.OutputScriptPath.IsNotNullOrEmpty())
            {
                File.WriteAllText(configuration.OutputScriptPath, scriptContents);
            }
            else
            {
                using (TransactionScope transaction = new TransactionScope(TransactionScopeOption.Required, TimeSpan.Zero))
                {
                    string[] scripts = Regex.Split(scriptContents, @"^\s*GO\s*$", RegexOptions.Multiline | RegexOptions.Compiled | RegexOptions.IgnoreCase);

                    foreach (string script in scripts)
                    {
                        if (script.Trim().IsNullOrEmpty())
                            continue;

                        System.Diagnostics.Debug.WriteLine("SCRIPT");
                        System.Diagnostics.Debug.WriteLine("============================================================================================================");
                        System.Diagnostics.Debug.Write(script);
                        System.Diagnostics.Debug.WriteLine("============================================================================================================");

                        using (SqlConnection connection = new SqlConnection(ConfigurationManager.ConnectionStrings[configuration.ConnectionStringName].ConnectionString))
                        {
                            connection.Open();

                            using (SqlCommand command = new SqlCommand())
                            {
                                command.CommandTimeout = 36000;
                                command.Connection = connection;
                                command.CommandType = CommandType.Text;
                                command.CommandText = script;
                                command.ExecuteNonQuery();
                            }
                        }
                    }

                    transaction.Complete();
                }
            }
        }

        private static string GetScript(SqlConfiguration configuration)
        {
            StringBuilder script = new StringBuilder();

            if(configuration.SchemaName.IsNotNullOrEmpty())
            {
                script.AppendLine("IF NOT EXISTS (SELECT * FROM sys.schemas WHERE name = N'{0}')".FormatWith(configuration.SchemaName));
                script.AppendLine("EXEC ('CREATE SCHEMA {0} AUTHORIZATION [dbo]');".FormatWith(configuration.SchemaName));
                script.AppendLine("GO");
            }

            string schema = configuration.SchemaName.IsNullOrEmpty() ? "dbo" : configuration.SchemaName;

            script.Append(
                @"
                IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[{0}].[UrlToProfile]') AND type in (N'U'))
                BEGIN
                    CREATE TABLE [{0}].[UrlToProfile](
	                    [Id] [uniqueidentifier] NOT NULL,
	                    [Url] [varchar](900) NOT NULL CONSTRAINT UQ_UrlToProfile_Url UNIQUE,
	                    [ProfilingCount] [bigint] NULL,
	                    [Server] [nvarchar](128) NULL,
	                    [Enabled] [bit] NOT NULL DEFAULT 0
                    CONSTRAINT PK_UrlToProfile PRIMARY KEY NONCLUSTERED 
                    (
	                    [Id] ASC
                    )WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]
                    ) ON [PRIMARY]
                END
                GO

                IF NOT EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID(N'[{0}].[UrlToProfile]') AND name = N'IX_UrlToProfile_Url')
                BEGIN
                    CREATE UNIQUE CLUSTERED INDEX [IX_UrlToProfile_Url] ON [{0}].[UrlToProfile]
                    (
	                    [Url] ASC
                    )
                END
                GO

                IF NOT EXISTS (SELECT * FROM dbo.sysobjects WHERE name = N'DF_UrlToProfile_Id')
                BEGIN
                    ALTER TABLE [{0}].[UrlToProfile] ADD CONSTRAINT [DF_UrlToProfile_Id]  DEFAULT (newid()) FOR [Id]
                END
                GO
            ".FormatWith(schema));

            script.Append(
                @"
                IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[{0}].[ProfiledRequestData]') AND type in (N'U'))
                BEGIN
                    CREATE TABLE [{0}].[ProfiledRequestData](
	                    [Id] [uniqueidentifier] NOT NULL,
                        [SessionId] [uniqueidentifier] NULL,
                        [SessionUserId] [nvarchar](128) NULL,
                        [SamplingId] [uniqueidentifier] NULL,
	                    [Url] [varchar](900) NOT NULL,
	                    [Data] [varbinary](max) NOT NULL,
                        [CapturedOnUtc] [DATETIME] NOT NULL
                    CONSTRAINT PK_ProfiledRequestData PRIMARY KEY NONCLUSTERED 
                    (
	                    [Id] ASC
                    )WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]
                    ) ON [PRIMARY]
                END
                GO

                IF NOT EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID(N'[{0}].[ProfiledRequestData]') AND name = N'IX_UrlToProfileData_Url')
                BEGIN
                    CREATE CLUSTERED INDEX [IX_UrlToProfileData_Url] ON [{0}].[ProfiledRequestData]
                    (
	                    [Url] ASC
                    )
                END
                GO
            ".FormatWith(schema));

            script.Append(
                @"
                IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[{0}].[ProfiledResponse]') AND type in (N'U'))
                BEGIN
                    CREATE TABLE [{0}].[ProfiledResponse](
	                    [Id] [uniqueidentifier] NOT NULL,
	                    [Url] [varchar](900) NOT NULL,
	                    [Body] [nvarchar](max) NOT NULL
                    CONSTRAINT PK_ProfiledResponse PRIMARY KEY NONCLUSTERED 
                    (
	                    [Id] ASC
                    )WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]
                    ) ON [PRIMARY]
                END
                GO

                IF NOT EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID(N'[{0}].[ProfiledResponse]') AND name = N'IX_ProfiledResponse_Url')
                BEGIN
                    CREATE CLUSTERED INDEX [IX_ProfiledResponse_Url] ON [{0}].[ProfiledResponse]
                    (
	                    [Url] ASC
                    )
                END
                GO
            ".FormatWith(schema));

            script.Append(
                @"
                IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[{0}].[TimedRequest]') AND type in (N'U'))
                BEGIN
                    CREATE TABLE [{0}].[TimedRequest](
	                    [Id] [uniqueidentifier] NOT NULL,
	                    [Url] [varchar](900) NOT NULL,
	                    [UrlPathAndQuery] [varchar](900) NULL,
	                    [DurationMs] BIGINT NULL,
	                    [Server] [varchar](128) NULL,
	                    [RequestUtc] DATETIME NULL
                    CONSTRAINT PK_TimedRequestId PRIMARY KEY NONCLUSTERED 
                    (
	                    [Id] ASC
                    )WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]
                    ) ON [PRIMARY]
                END
                GO
            ".FormatWith(schema));

            return script.ToString();
        }
    }
}
