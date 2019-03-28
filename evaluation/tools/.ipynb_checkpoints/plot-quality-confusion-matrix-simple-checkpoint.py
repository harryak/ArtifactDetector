import seaborn as sns
import pandas as pds
import matplotlib.pyplot as plt
import numpy as np
import csv
import pprint

sns.set()

artifact_results = {}

with open('output_qualitative-02_mod.csv','r') as csvfile:
    plots = csv.reader(csvfile, delimiter=';')

    i = 0
    for row in plots:
        # Skip first row.
        if i == 0:
            i += 1
            continue

        # Make sure the values are accessible.
        if row[4] not in artifact_results:
            artifact_results[row[4]] = {"index": len(artifact_results), "tp": 0, "tn": 0, "fp": 0, "fn": 0, "True": 0, "False": 0}

        # Classify match.
        if row[9] == row[11]:
            if row[9] == "True":
                artifact_results[row[4]]["tp"] += 1
            else:
                artifact_results[row[4]]["tn"] += 1
        else:
            if row[9] == "True":
                artifact_results[row[4]]["fp"] += 1
            else:
                artifact_results[row[4]]["fn"] += 1

        # Count total up
        artifact_results[row[4]][row[11]] += 1

        i += 1

outputMatrix = []
outputMatrix.append([])
outputMatrix.append([])
outputMatrix.append([])
outputMatrix.append([])

annotations = []
annotations.append([])
annotations.append([])
annotations.append([])
annotations.append([])

# Go through entries sorted by their appearance
for result in sorted(list(artifact_results.values()), key=lambda kv: kv["index"]):
    outputMatrix[0].append(result["tp"] / result["True"])
    outputMatrix[1].append(result["tn"] / result["False"])
    outputMatrix[2].append(result["fp"] / result["False"])
    outputMatrix[3].append(result["fn"] / result["True"])
    annotations[0].append(str(result["tp"]) + "\n" + str(result["True"]))
    annotations[1].append(str(result["tn"]) + "\n" + str(result["False"]))
    annotations[2].append(str(result["fp"]) + "\n" + str(result["False"]))
    annotations[3].append(str(result["fn"]) + "\n" + str(result["True"]))

df_cm = pds.DataFrame(outputMatrix, index = ["True positive", "True negative", "False positive", "False negative"],
                    columns = range(1, len(artifact_results)))

#print(np.array(annotations))
plt.figure(figsize=(25, 4))
sns.heatmap(df_cm, annot=np.array(annotations), fmt="s", vmin=0, vmax=1, cmap="RdYlGn", linewidths=.5, square=True, cbar=False)
plt.show()