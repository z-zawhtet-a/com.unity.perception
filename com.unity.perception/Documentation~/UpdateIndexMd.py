# This script has two functions:
# Without the -check parameter it updates com.unity.perception/Documentation~/Index.md with the content in README.md
# With the -check parameter it checks com.unity.perception/Documentation~/Index.md to make sure it is up to date with README.md
import argparse
import re

parser = argparse.ArgumentParser()

parser.add_argument("-check", help="Check that Index.md is up to date with README.md", action='store_true')

args = parser.parse_args()

with open("../../README.md", "r") as f:
    content = f.read()

content_updated = content.replace("com.unity.perception/Documentation~/", "")

def excluderepl(matchobj):
    if matchobj.group(0).count(r'[//]: # (End Exclude)') > 1:
        return matchobj.group(0)
    else:
        return ''

content_updated = re.sub('\[\/\/\]\: \# \(Exclude from Index\.md\).*?\[\/\/\]\: \# \(End Exclude\)', excluderepl, content_updated, flags=re.DOTALL)

if args.check:
    with open("Index.md", "r") as indexFile:
        indexContent = indexFile.read()

    if indexContent == content_updated:
        exit(0)
    else:
        print("com.unity.perception/Documentation~/Index.md is not up to date. Run com.unity.perception/Documentation~/UpdateIndexMd.py to update it.")
        exit(1)
else:
    with open("Index.md", "w") as indexFile:
        indexFile.write(content_updated)