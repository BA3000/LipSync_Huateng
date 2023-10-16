# -*-coding:utf-8 -*-

"""
File    : anim2Json.py
Time    : 2023/10/17
Author  : yanhuiwu
"""

import os
import json

### config
INPUT_FILE_PATH = "data\\test_demo\\rjbwks6091"

def main():
    inputFileName = os.path.basename(INPUT_FILE_PATH)
    inputPath = os.path.dirname(INPUT_FILE_PATH)
    outContent = dict()
    outContent["data"] = []
    if os.path.exists(INPUT_FILE_PATH):
        print("INFO: path valid, converting")
        for line in open(INPUT_FILE_PATH):
            outContent["data"].append(json.loads(line.split("#")[2]))
    else:
        print("ERR: PATH DOES NOT EXIST!")
    res = json.dumps(outContent)
    outFileName = inputFileName.split(".")[0] + ".json"
    outFilePath = os.path.join(inputPath, outFileName)
    with open(outFilePath, 'w') as f:
        f.write(res)

if __name__ == "__main__":
    main()
