import numpy as np
import matplotlib.pyplot as plt
import csv
from operator import add

x = []

artifacttype_retrieved  = []
features_extracted      = []
matching_finished       = []
total                   = []
std_dev                 = []

with open('output_qualitative-02.csv','r') as csvfile:
    plots = csv.reader(csvfile, delimiter=';')

    i = 0
    for row in plots:
        if i == 0:
            i += 1
            continue

        x.append(i)
        artifacttype_retrieved.append(float(row[3].replace(",", ".")))
        features_extracted.append(float(row[4].replace(",", ".")))
        matching_finished.append(float(row[5].replace(",", ".")))
        total.append(float(row[6].replace(",", ".")))
        i += 1

std_dev = np.std(total, axis=0)

plt.bar(x, matching_finished)
plt.bar(x, features_extracted, bottom=matching_finished)
plt.bar(x, artifacttype_retrieved, bottom=list(map(add, matching_finished, features_extracted)))
plt.show()