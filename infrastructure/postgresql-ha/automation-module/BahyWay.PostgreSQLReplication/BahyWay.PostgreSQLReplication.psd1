@{
    # Module Manifest for BahyWay PostgreSQL Replication Automation
    # Module version
    ModuleVersion = '1.0.0'

    # Unique ID for this module
    GUID = '8f3c5d2e-1a4b-9c7d-8e6f-3a2b1c4d5e6f'

    # Author of this module
    Author = 'BahyWay Development Team'

    # Company or vendor of this module
    CompanyName = 'BahyWay'

    # Copyright statement
    Copyright = '(c) 2024 BahyWay. All rights reserved.'

    # Description of functionality provided by this module
    Description = 'PowerShell module for automating PostgreSQL HA replication cluster management using Docker Compose. Provides commands to start, stop, monitor, and manage PostgreSQL primary-replica clusters for BahyWay projects.'

    # Minimum version of PowerShell required
    PowerShellVersion = '7.0'

    # Root module file
    RootModule = 'BahyWay.PostgreSQLReplication.psm1'

    # Functions to export from this module
    FunctionsToExport = @(
        'Initialize-PostgreSQLCluster',
        'Start-PostgreSQLCluster',
        'Stop-PostgreSQLCluster',
        'Restart-PostgreSQLCluster',
        'Remove-PostgreSQLCluster',
        'Get-PostgreSQLClusterStatus',
        'Show-PostgreSQLClusterLogs',
        'Test-PostgreSQLReplication',
        'Get-PostgreSQLReplicationLag',
        'Connect-PostgreSQLPrimary',
        'Connect-PostgreSQLReplica',
        'Invoke-PostgreSQLQuery',
        'Backup-PostgreSQLCluster',
        'Restore-PostgreSQLCluster'
    )

    # Cmdlets to export from this module
    CmdletsToExport = @()

    # Variables to export from this module
    VariablesToExport = @()

    # Aliases to export from this module
    AliasesToExport = @(
        'pgstart',
        'pgstop',
        'pgrestart',
        'pgstatus',
        'pglogs',
        'pgtest'
    )

    # Private data to pass to the module
    PrivateData = @{
        PSData = @{
            Tags = @('PostgreSQL', 'Docker', 'Replication', 'HA', 'BahyWay', 'Database', 'Automation')
            ProjectUri = 'https://github.com/bahyway/StepByStepLab'
            ReleaseNotes = @'
Version 1.0.0
- Initial release
- Docker Compose-based PostgreSQL HA cluster management
- Automatic primary-replica replication setup
- Health monitoring and status checking
- Replication lag monitoring
- Backup and restore capabilities
'@
        }
    }
}
