﻿using System;

namespace ArbitraryArtifactDetector.DetectorCondition.Model
{
    class GreaterThanEqualDetectorCondition<ObjectType> : BaseDetectorCondition<ObjectType, IComparable>, IDetectorCondition<ObjectType>
    {
        public GreaterThanEqualDetectorCondition(string aspectToCheck, IComparable greaterThan)
            : base(aspectToCheck, (IComparable aspect, Type aspectType) => aspect.CompareTo(Convert.ChangeType(greaterThan, aspectType)) >= 0)
        {
            AspectToCheck = aspectToCheck;
        }
    }
}