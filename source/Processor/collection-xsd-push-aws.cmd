


rem -- DRY RUN -- Verify this is correct and hit enter
rem --
rem -- copy current files to s3
rem
aws s3 cp --dryrun ./Collection.xsd s3://contensive/xsd
pause

aws s3 cp ./Collection.xsd s3://contensive/xsd

aws s3 ls s3://contensive/xsd 

pause