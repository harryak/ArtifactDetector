import seaborn as sns
import pandas as pds
import matplotlib.pyplot as plt
import csv
import pprint

sns.set()
x = []

goal_types = {}
query_types = {}
#results = []
#goal_results = []
dataMatrix = {}

with open('output_qualitative-02_mod.csv','r') as csvfile:
    plots = csv.reader(csvfile, delimiter=';')

    i = 0
    for row in plots:
        # Skip first row.
        if i == 0:
            i += 1
            continue

        # Use artifact_visible as indexer.
        # row[3] = artifact_visible
        if row[3] == "not_artifacts-":
            row[3] = "not_artifacts"

        if row[3] not in goal_types:
            goal_types[row[3]] = {"Index": len(goal_types), "True": len(goal_types) * 2, "False": len(goal_types) * 2 + 1}

        if row[4] not in query_types:
            query_types[row[4]] = {"Index": len(query_types), "True": len(query_types) * 2, "False": len(query_types) * 2 + 1}

        if query_types[row[4]][row[9]] not in dataMatrix:
            dataMatrix[query_types[row[4]][row[9]]] = {}

        # New dataset for artifacttype_goal?
        # row[4] = artifacttype_goal
        if goal_types[row[3]][row[11]] not in dataMatrix[query_types[row[4]][row[9]]]:
            dataMatrix[query_types[row[4]][row[9]]][goal_types[row[3]][row[11]]] = 0

        # Classify match
        if row[9] == row[11]:
            dataMatrix[query_types[row[4]][row[9]]][goal_types[row[3]][row[11]]] += 1
        else:
            dataMatrix[query_types[row[4]][row[9]]][goal_types[row[3]][row[11]]] -= 1

        #dataMatrix[row[4]][goal_types[row[3]]]["found"] += row[9]
        #dataMatrix.append([row[9],row[11]])#row[4],row[3],
        #artifacttypes.append(row[4])
        #goal_types.append(row[3])
        #results.append(row[9])
        #goal_results.append(row[11])

        i += 1

for qtypes in query_types:
    for qswitch in query_types[qtypes]:
        for gtypes in goal_types:
            for gswitch in goal_types[gtypes]:
                if goal_types[gtypes][gswitch] not in dataMatrix[query_types[qtypes][qswitch]]:
                    dataMatrix[query_types[qtypes][qswitch]][goal_types[gtypes][gswitch]] = 0

outputMatrix = []
for datarow in dataMatrix:
    outputMatrix.append(list(dataMatrix[datarow].values()))

xIndexDict = {}
for queryKeys in query_types:
    xIndexDict[query_types[queryKeys]["Index"]] = [queryKeys + "_True", queryKeys + "_False"]
xIndex = list(xIndexDict.values())
xIndex = [items for sublist in xIndex for items in sublist]

yIndexDict = {}
for goalKeys in goal_types:
    yIndexDict[goal_types[goalKeys]["Index"]] = [goalKeys + "_True", goalKeys + "_False"]
yIndex = list(yIndexDict.values())
yIndex = [items for sublist in yIndex for items in sublist]

df_cm = pds.DataFrame(outputMatrix, index = xIndex,
                    columns = yIndex)
plt.figure(figsize=(10, 10))
sns.heatmap(df_cm, annot=True)
plt.show()