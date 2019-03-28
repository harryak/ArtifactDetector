# -*- coding: utf-8 -*-
"""
@author: Felix Rossmann <rossmann@cs.uni-bonn.de>
"""

import seaborn as sns
import pandas as pds
import matplotlib.pyplot as plt


dataMatrix = []

pathsDictStyle ={
            "C:\\Users\\Lab19\\Desktop\\evaluation_quantitative\\artifact_absent.png": "Artifact absent",
            "C:\\Users\\Lab19\\Desktop\\evaluation_quantitative\\artifact_present.png": "Artifact present"
        }

namesDictX = {
        "artifact_00_images": "00",
        "artifact_01_images": "01",
        "artifact_02_images": "02",
        "artifact_03_images": "03",
        "artifact_04_images": "04",
        "artifact_05_images": "05",
        "artifact_06_images": "06",
        "artifact_07_images": "07",
        }

def toFloat(x):
    return float(x.replace(',','.'))

conv = {
        1: lambda x: pathsDictStyle[x],
        2: lambda x: namesDictX[x],
        3: toFloat,
        4: toFloat,
        5: toFloat,
        6: toFloat
        }
df = pds.read_csv("output_quantitative-01.csv", sep=";", converters=conv, usecols=[1,2,3,4,5,6], names=["Provided screenshot", "Searched artifact", "Load references [ms]", "Feature extraction [ms]", "Matching time [ms]", "Total time [ms]"], skiprows=[0], header=None)

sns.set(style="whitegrid", font_scale=1.5)
sns.catplot(x="Searched artifact", y="Matching time [ms]", hue="Provided screenshot", kind="violin", split=True, inner=None, bw=.5, data=df, saturation=1)
plt.show()
