﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Dreamteck.Splines.Primitives
{
    public class Rectangle : SplinePrimitive
    {
        public Vector2 size = Vector2.one;

        public override Spline.Type GetSplineType()
        {
            return Spline.Type.Linear;
        }

        protected override void Generate()
        {
            base.Generate();
            closed = true;
            CreatePoints(5, SplinePoint.Type.SmoothMirrored);
            points[0].position = points[0].tangent = Vector3.forward / 2f * size.y + Vector3.left / 2f * size.x;
            points[1].position = points[1].tangent = Vector3.forward / 2f * size.y + Vector3.right / 2f * size.x;
            points[2].position = points[2].tangent = Vector3.back / 2f * size.y + Vector3.right / 2f * size.x;
            points[3].position = points[3].tangent = Vector3.back / 2f * size.y + Vector3.left / 2f * size.x;
            points[4] = points[0];
        }
    }
}