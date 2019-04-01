# -*- coding: utf-8 -*-
"""
@author: Felix Rossmann <rossmann@cs.uni-bonn.de>
"""

import seaborn as sns
import pandas as pds
import matplotlib.pyplot as plt


dataMatrix = []

outputMap = {
            "True": "Artifact found",
            "False": "Artifact not found",
        }

namesDictX = {
            "01_Jibberish-Mittel": "A1",
            "02_Link_Only-Schwer": "A2",
            "03_Targo_Bank-Schwer": "A3",
            "04_Targeted_IT_Abteilung-Einfach": "A4",
            "04_Targeted_IT_Abteilung-Schwer": "A5",
            "05_Newsletter-Schwer": "A6",
            "06_Versichertenkarte-Schwer": "A7",
            "07_Paypal-Mittel": "A8",
            "08_Weiterbildung-Schwer": "A9",
            "12_IT_Ticket-Mittel": "A10",
            "browser_advertisement": "A11",
            "browser_defacing": "A12",
            "email-bounce-exchange-de": "A13",
            "exe_anti_virus_extended": "A14",
            "exe_anti_virus_simple": "A15",
            "exe_file_scanner": "A16",
            "exe_login_window": "A17",
            "exe_self_remove": "A18",
            "exe_updater_generic": "A19",
            "exe_updater_human": "A20",
            "exe_updater_java": "A21",
            "exe_updater_remote": "A22",
            "exe_updater_simple": "A23",
            "ms_word_macro": "A24",
            "ms_word_protected_view": "A25"
        }

def toFloat(x):
    return float(x.replace(',','.'))

conv = {
        2: lambda x: namesDictX[x],
        3: toFloat,
        4: toFloat,
        5: toFloat,
        6: toFloat,
        7: lambda x: outputMap[x]
        }
df = pds.read_csv("output_qualitative-03.csv", sep=";", converters=conv, usecols=[2,3,4,5,6,7], names=["Searched artifact", "Load references [ms]", "Feature extraction [ms]", "Matching time [ms]", "Total time [ms]", "Computed result"], skiprows=[0], header=None)

#df["Category"] = df["Computed result"] == df["Correct result"]
#print(df)

sns.set(style="whitegrid", font_scale=1.5)
ax = sns.catplot(x="Searched artifact", y="Total time [ms]", hue="Computed result", kind="violin", split=True, inner=None, bw=.5, data=df, saturation=1, sharex=False, sharey=False, aspect=2.5)
axes = ax.axes
axes[0,0].set_ylim(0, 2500)

plt.savefig("runtime-qualitative-03.svg")
