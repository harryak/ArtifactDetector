import argparse
import os
from PIL import Image

parser = argparse.ArgumentParser(description='On base of two image sets compose an evaluation image set.')
parser.add_argument('--bg', dest='bgs', action='store',
                    help='Folder of background images (screenshots).')
parser.add_argument('--fg', dest='fgs', action='store',
                    help='Folder of window images (screenshots).')

args = parser.parse_args()
if args.bgs == None or args.fgs == None:
    print('ERROR: Please provide parameters.')
    print()
    parser.print_help()
    exit()

if not os.path.isdir(args.bgs) or not os.path.isdir(args.fgs):
    print('ERROR: At least one folder is not existent.')
    print()
    parser.print_help()
    exit()

print('Using folders ', args.bgs, ' and ', args.fgs)