

rem -- copy this script to the local deployment folder and it will upload the current folder to the s3 current deployment folder
rem --
rem -- MUST BE IN DEPLOYMENT FOLDER WITH FILES
rem --
rem -- STOP AND CHECK THE FOLDER
rem --

pause


rem -- DRY RUN -- Verify this is correct and hit enter
rem --
rem -- remove all files from the deployment folder
rem --
aws s3 rm --dryrun s3://contensive/current --recursive
pause
aws s3 rm s3://contensive/current --recursive


rem -- DRY RUN -- Verify this is correct and hit enter
rem --
rem -- copy current files to s3
rem
aws s3 cp --dryrun . s3://contensive/current --recursive
pause
aws s3 cp . s3://contensive/current --recursive


rem -- list files in the deployment folder
aws s3 ls s3://contensive/current --recursive

pause