from typing import List, Dict, DefaultDict
import sys
from os import path
import os
from pprint import pprint
from collections import defaultdict
from random import shuffle

def char_range(start, end):
    l = list(map(chr, range(ord(start), ord(end) + 1)))
    shuffle(l)
    return l

SPECIAL = ['~', '`', '!', '@', '#', '$', '%', '^', '&', '*', '(', ')', '-', '_', '+', '=', '[', ']', '{', '}', '\\', '|', ';', ':', '"', '\'', '<', '>', ',', '.', '?', '/']

def count_chars(counts: DefaultDict[str, int], file_path: str):
    with open(file_path, 'r') as f:
        for line in f.readlines():
            for c in line:
                counts[c] += 1
    

def limit_chars(counts: DefaultDict[str, int]):
    keys = list(counts.keys())

    for k in keys:
        if 'a' <= k <= 'z':
            continue
            
        if 'A' <= k <= 'Z':
            continue

        if '0' <= k <= '9':
            continue

        if k in SPECIAL:
            continue

        del counts[k]

def build_replace_map(counts: DefaultDict[str, int]) -> Dict[str, str]:
    replacements = {}

    groups = {
        '0': char_range('0', '9'),
        'a': char_range('a', 'z'),
        'A': char_range('A', 'Z'),
        '/': list(SPECIAL)
    }

    for (k, v) in sorted(counts.items(), key=lambda x: x[1]):
        r = None
        if 'a' <= k <= 'z':
            r = groups['a'].pop(0)
            
        if 'A' <= k <= 'Z':
            r = groups['A'].pop(0)

        if '0' <= k <= '9':
            r = groups['0'].pop(0)
        
        if k in SPECIAL:
            r = groups['/'].pop(0)

        replacements[k] = r

    return replacements

def rewrite_line(line: str, replacement: Dict[str, str]):
    return ''.join([replacement.get(c, c) for c in line])

def rewrite(input_file: str, output_file: str, replacement: Dict[str, str]):
    os.rename(output_file, input_file)

    with open(input_file, 'r') as inp:
        with open(output_file, 'w') as out:
            for l in inp.readlines():
                out.write(rewrite_line(l, replacement))
    

def main(directory: str, input_files: List[str]):
    input_paths = [path.join(directory, x) for x in input_files]

    counts = defaultdict(lambda: 0)
    for f in input_paths:
        count_chars(counts, f)

    limit_chars(counts)

    replace_map = build_replace_map(counts)

    for f in input_paths:
        rewrite(path.splitext(f)[0] + '.bak', f,  replace_map)

main(sys.argv[1], sys.argv[2:])