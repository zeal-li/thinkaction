@echo off
setlocal enabledelayedexpansion

::±£´æµ±Ç°Ä¿Â¼
set cur_work_path=%cd%

::»ñÈ¡²ÎÊý
set script_path=%~dp0
set sql_file_path=%script_path%sql
set mysql_name=%1
set execute_command=%2
set execute_db_name=%3
set execute_db_table_sql=%4
set execute_db_change_sql=%5

if "%execute_command%" == "create" (
    call :mysql_operate_create !execute_db_name! !execute_db_table_sql!
) else if "%execute_command%" == "delete" (
    call :mysql_operate_delete !execute_db_name!
) else if "%execute_command%" == "update" (
    call :mysql_operate_update !execute_db_name! !execute_db_change_sql!
) else if "%execute_command%" == "clear" (
    call :mysql_operate_clear !execute_db_name! !execute_db_table_sql!
) else if "%execute_command%" == "adduser" (
    call :execute_adduser
) else (
    echo Î´¶¨ÒåMySQL Commond:%execute_command%
    exit /b 1
)

::»¹Ô­µ±Ç°Ä¿Â¼
cd %cur_work_path%
exit /b 0


::Mysql µ¼ÈëÎÄ¼þ %1[execute_db_name] %2[sql_file]
:execute_mysql_loadfile
    set eml_user_name=root
    set eml_user_passwd=
    set eml_host=localhost
    set eml_db_name=%1
    set eml_sql_file=%2
    echo µ¼Èëmysql¸üÐÂSQLÎÄ¼þ£º!eml_sql_file!
    call %script_path%%mysql_name%/bin/mysql.exe -u%eml_user_name% -h%eml_host% -D !eml_db_name! < !eml_sql_file!
    exit /b %errorlevel%
    goto:EOF

::Mysql Ö´ÐÐÃüÁî %[execute_command]
:execute_mysql_command
    set emc_user_name=root
    set emc_user_passwd=
    set emc_host=localhost
    set emc_command=%1
    echo Ö´ÐÐmysqlÃüÁî£º!emc_command!
    call %script_path%%mysql_name%/bin/mysql.exe -u%emc_user_name% -h%emc_host% -e !emc_command!
    exit /b %errorlevel%
    goto:EOF

::Ö´ÐÐ´´¿â %1[execute_db_name] %2[execute_db_table_sql]
:mysql_operate_create
    ::Ö´ÐÐÃüÁî
    set mysql_operate_crate_db_name=%1
    set mysql_operate_creage_table_sql=!sql_file_path!\%2
    call :execute_mysql_command "USE %mysql_operate_crate_db_name%;"
    if !errorlevel! equ 0 (
        echo [31mµ±Ç°mysqlÊµÀýÒÑ´æÔÚÊý¾Ý¿â£º%mysql_operate_crate_db_name%[0m
        exit /b 1
    )
    call :execute_mysql_command "CREATE DATABASE IF NOT EXISTS %mysql_operate_crate_db_name%;"
    call :execute_mysql_loadfile %mysql_operate_crate_db_name% !mysql_operate_creage_table_sql!

    exit /b 0
    goto:EOF

::Ö´ÐÐÉ¾¿â %1[execute_db_name]
:mysql_operate_delete
    ::Ö´ÐÐÃüÁî
    set mysql_operate_delete_db_name=%1
    call :execute_mysql_command "USE %mysql_operate_delete_db_name%;"
    if !errorlevel! neq 0 (
        echo [31mµ±Ç°mysqlÊµÀý²»´æÔÚÊý¾Ý¿â£º%mysql_operate_delete_db_name%[0m
        exit /b 1
    )
    call :execute_mysql_command "DROP DATABASE IF EXISTS %mysql_operate_delete_db_name%;"

    exit /b 0
    goto:EOF

::Ö´ÐÐ¸üÐÂ %1[execute_db_name] %2[execute_db_change_sql]
:mysql_operate_update
    ::Ö´ÐÐÃüÁî
    set mysql_operate_update_db_name=%1
    set mysql_operate_update_change_sql=!sql_file_path!\%2
    call :execute_mysql_command "USE %mysql_operate_update_db_name%;"
    if !errorlevel! neq 0 (
        echo [31mµ±Ç°mysqlÊµÀý²»´æÔÚÊý¾Ý¿â£º%mysql_operate_update_db_name%[0m
        exit /b 1
    )
    call :execute_mysql_loadfile %mysql_operate_update_db_name% !mysql_operate_update_change_sql!

    exit /b 0
    goto:EOF

::Ö´ÐÐÇå¿ÕÊý¾Ý¿â %1[execute_db_name] %2[execute_db_table_sql]
:mysql_operate_clear
    ::Ö´ÐÐÃüÁî
    set mysql_operate_clear_db_name=%1
    set mysql_operate_clear_table_name=!sql_file_path!\%2
    call :execute_mysql_command "USE %mysql_operate_clear_db_name%;"
    if !errorlevel! neq 0 (
        echo [31mµ±Ç°mysqlÊµÀý²»´æÔÚÊý¾Ý¿â£º%mysql_operate_clear_db_name%[0m
        exit /b 1
    )
    call :execute_mysql_command "DROP DATABASE IF EXISTS %mysql_operate_clear_db_name%;CREATE DATABASE IF NOT EXISTS %mysql_operate_clear_db_name%;"
    call :execute_mysql_loadfile %mysql_operate_clear_db_name% !mysql_operate_clear_table_name!

    exit /b 0
    goto:EOF

::Mysql Ìí¼ÓÓÃ»§
:execute_adduser
    set default_user_name=block
    set default_user_passwd=block
    set default_user_host=%%%%%%%%

    if defined local_mysql_default_user (
        set default_user_name=%local_mysql_default_user%
    )
    if defined local_mysql_default_passwd (
        set default_user_passwd=%local_mysql_default_passwd%
    )

    choice /c YNC /M "Ä¬ÈÏÓÃ»§Ãû[%default_user_name%],ÊÇ·ñÐÞ¸Ä
    if %errorlevel% == 1 (
        set /p default_user_name=ÇëÊäÈëÓÃ»§Ãû:
    ) else if %errorlevel% == 3 (
        exit /b 0
    )
    choice /c YNC /M "Ä¬ÈÏÓÃ»§ÃÜÂë[%default_user_passwd%],ÊÇ·ñÐÞ¸Ä
    if %errorlevel% == 1 (
        set /p default_user_passwd=ÇëÊäÈëÓÃ»§ÃÜÂë:
    ) else if %errorlevel% == 3 (
        exit /b 0
    )
    choice /c YN /M "È·ÈÏ´´½¨ ÓÃ»§[%default_user_name%] ÃÜÂë[%default_user_passwd%]
    if %errorlevel% == 2 (
        exit /b 0
    )

    set create_user_command="CREATE USER '%default_user_name%'@'!default_user_host!' IDENTIFIED BY '%default_user_passwd%';"
    set grant_command="GRANT ALL PRIVILEGES ON *.* TO '%default_user_name%'@'!default_user_host!'"
    set flush_command="FLUSH PRIVILEGES;"
    call :execute_mysql_command !create_user_command!
    call :execute_mysql_command !grant_command!
    call :execute_mysql_command !flush_command!
    goto:EOF

