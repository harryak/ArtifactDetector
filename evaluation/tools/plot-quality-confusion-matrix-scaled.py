# -*- coding: utf-8 -*-
"""
@author: Felix Rossmann <rossmann@cs.uni-bonn.de>
"""

import seaborn as sns
import pandas as pds
import matplotlib as mpl
import matplotlib.pyplot as plt
import numpy as np

sns.set(font_scale=1.5)

mpl.rcParams["text.usetex"] = True
mpl.rcParams["text.latex.preamble"] = [r"\usepackage{amsmath}"]

def mapArtifactName(x):
    if x == "not_artifacts-":
        return "not_artifacts"
    else:
        return x

def mapBool(x):
    if x == "True":
        return 1
    else:
        return 0

converters = {
        3: mapArtifactName,
        9: mapBool,
        11: mapBool
        }
df = pds.read_csv("output_qualitative-scaled-01_mod.csv", converters=converters, sep=";", usecols=[3,4,9,11], names=["Artifact present", "Artifact queried", "Detection result", "Actual result"], skiprows=[0], header=None)

def decideResult(drlist, arlist):
    result = []
    for i in range(0, len(drlist)):
        dr = drlist[i]
        ar = arlist[i]
        
        if dr == ar:
            if dr == 1:
                result.append("TP")
            else:
                result.append("TN")
        else:
            if dr == 1:
                result.append("FP")
            else:
                result.append("FN")
    return result
    
def mapVals(x):
    if x != x:
        return 0
    else:
        return 1
    
df = df.assign(Result=decideResult(df["Detection result"], df["Actual result"]))
df = df.pivot(columns="Result", values=lambda x: mapVals(x["Result"]))
#df = df["FN"].sum()

print(df)

#print(np.array(annotations))
#plt.figure(figsize=(2, 2))
#ax = sns.heatmap(df, annot="", annot_kws={"size": 24}, fmt="s", vmin=0, vmax=1, cmap="RdYlGn", linewidths=.5, square=True, cbar=False)
#ax.tick_params(labelbottom=False,labeltop=True)
#ax.tick_params(axis="y", labelrotation=0)
#ax.set_xlabel("Artifact")
#ax.set_ylabel("$\\frac{\\text{sum measured}}{\\text{sum goal}}$")
#plt.show()
