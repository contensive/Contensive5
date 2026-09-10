DECLARE @DBName varchar(255)

DECLARE @DATABASES_Fetch int

DECLARE DATABASES_CURSOR CURSOR FOR
    select
        DATABASE_NAME   = db_name(s_mf.database_id)
    from
        sys.master_files s_mf
    where
       -- ONLINE
        s_mf.state = 0 

       -- Only look at databases to which we have access
    and has_dbaccess(db_name(s_mf.database_id)) = 1 

        -- Not master, tempdb or model
    and db_name(s_mf.database_id) not in ('Master','tempdb','model')
    group by s_mf.database_id
    order by 1

OPEN DATABASES_CURSOR

FETCH NEXT FROM DATABASES_CURSOR INTO @DBName

WHILE @@FETCH_STATUS = 0
BEGIN
    declare @DBFileName varchar(256)    
    set @DBFileName = replace(replace(@DBName,':','_'),'\','_')

    exec ('BACKUP DATABASE [' + @DBName + '] TO  DISK = N''d:\DbBackup\' + 
        @DBFileName + ''' WITH NOFORMAT, INIT,  NAME = N''' + 
        @DBName + '-Full Database Backup'', SKIP, NOREWIND, NOUNLOAD,  STATS = 100')

    FETCH NEXT FROM DATABASES_CURSOR INTO @DBName
END

CLOSE DATABASES_CURSOR
DEALLOCATE DATABASES_CURSOR
