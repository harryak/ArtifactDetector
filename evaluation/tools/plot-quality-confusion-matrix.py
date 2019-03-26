import seaborn as sn
import pandas as pd
import matplotlib.pyplot as plt
import csv

x = []

artifacttypes = []
#goal_types = []
#results = []
#goal_results = []
dataMatrix = []

with open('output_qualitative-02.csv','r') as csvfile:
    plots = csv.reader(csvfile, delimiter=';')

    i = 0
    for row in plots:
        if i == 0:
            i += 1
            continue

        x.append(i)
        dataMatrix.append([row[9],row[11]])#row[4],row[3],
        artifacttypes.append(row[4])
        #goal_types.append(row[3])
        #results.append(row[9])
        #goal_results.append(row[11])
        i += 1

df_cm = pd.DataFrame(dataMatrix, index = ["result", "found"],
	columns = artifacttypes)
plt.figure(figsize)