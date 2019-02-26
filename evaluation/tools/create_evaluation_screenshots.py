import argparse
import os
import random
from PIL import Image

# Setup cli arguments.
parser = argparse.ArgumentParser(description='On base of two image sets compose an evaluation image set.')
parser.add_argument('--bg', dest='bgs', action='store',
                    help='Folder of background images (screenshots).')
parser.add_argument('--fg', dest='fgs', action='store',
                    help='Folder of window images (screenshots).')
parser.add_argument('--o', dest='outputFolder', action='store',
                    help='Output folder for evaluation screenshots.')

# Get and check arguments.
args = parser.parse_args()
if args.bgs == None or args.fgs == None or args.outputFolder == None:
    print('ERROR: Please provide parameters.')
    print()
    parser.print_help()
    exit()

if not os.path.isdir(args.bgs) or not os.path.isdir(args.fgs) or not os.path.isdir(args.outputFolder):
    print('ERROR: At least one folder is not existent.')
    print()
    parser.print_help()
    exit()

# Debug output.
print('Using folders ', args.bgs, ' and ', args.fgs)

# Go through all artifact types (each have one folder)
for artifactTypeFolder in os.listdir(args.fgs):
    artifactTypeName = artifactTypeFolder
    artifactTypeFolder = os.path.join(args.fgs, artifactTypeFolder)

    # Is this a folder?
    if os.path.isdir(artifactTypeFolder):
        # Reset counter of background screenshots.
        bgNumber = 0

        # For each background image:
        for bgFile in os.listdir(args.bgs):
            # Load screenshot.
            bgFile = os.path.join(args.bgs, bgFile)
            if os.path.isfile(bgFile):
                bgNumber += 1
                bgImage = Image.open(bgFile, 'r')
                bgImageWidth, bgImageHeight = bgImage.size
                
                fgNumber = 0

                # Paste each foreground image onto it.
                for fgFile in os.listdir(artifactTypeFolder):
                    # Load foreground screenshot.
                    fgFile = os.path.join(artifactTypeFolder, fgFile)
                    fgImage = Image.open(fgFile, 'r')
                    fgImageWidth, fgImageHeight = fgImage.size

                    # Calculate at which dimensions the image is still recognizable.
                    fgImageWidthRecognizable = int(float(fgImageWidth) * 0.9)
                    fgImageHeightRecognizable = int(float(fgImageHeight) * 0.9)

                    # Is the screenshot too large?
                    if fgImageWidthRecognizable > bgImageWidth or fgImageHeightRecognizable > bgImageHeight:
                        print('Screenshot too large: ', fgFile)
                        continue

                    # Count as new foreground.
                    fgNumber += 1

                    # Calculate range for random offset.
                    minOffsetX = 0 + fgImageWidthRecognizable - fgImageWidth
                    maxOffsetX = bgImageWidth - fgImageWidthRecognizable
                    minOffsetY = 0 + fgImageHeightRecognizable - fgImageHeight
                    maxOffsetY = bgImageHeight - fgImageHeightRecognizable

                    # Randomize offset
                    offset = (random.randint(minOffsetX, maxOffsetX), random.randint(minOffsetY, maxOffsetY))

                    # Compose image
                    canvas = Image.new('RGB', (bgImageWidth, bgImageHeight), (255, 255, 255))
                    canvas.paste(bgImage, (0, 0))
                    canvas.paste(fgImage, offset)
                    canvas.save(args.outputFolder + '/' + artifactTypeName + '-' + str(bgNumber) + '-' + str(fgNumber) + '.png')
