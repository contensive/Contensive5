

rem -- DRY RUN -- Verify this is correct and hit enter
rem --
rem -- copy dtd file to s3
rem
aws s3 cp --dryrun . s3://contensive/dtd --recursive --exclude "*" --include "*.dtd"

pause

aws s3 cp . s3://contensive/dtd --recursive --exclude "*" --include "*.dtd"

aws s3 ls s3://contensive/dtd --recursive

pause