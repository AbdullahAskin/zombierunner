namespace Dreamteck.Splines
{
    using System.Collections;
    using System.Collections.Generic;
    using UnityEngine;

    [System.Serializable]
    public class SampleCollection
    {
        [HideInInspector]
        public SplineSample[] samples = new SplineSample[0];
        public int Count
        {
            get { return samples.Length; }
        }
        public int[] optimizedIndices = new int[0];
        bool hasSamples
        {
            get { return Count > 0; }
        }
        public SplineComputer.SampleMode sampleMode = SplineComputer.SampleMode.Default;
        public double clipFrom = 0.0, clipTo = 1.0;

        public bool loopSamples = false;

        public bool samplesAreLooped
        {
            get
            {
                return loopSamples && clipFrom >= clipTo;
            }
        }
        public double span
        {
            get
            {
                if (samplesAreLooped) return (1.0 - clipFrom) + clipTo;
                return clipTo - clipFrom;
            }
        }

        public SampleCollection()
        {

        }

        public SampleCollection(SampleCollection input)
        {
            samples = input.samples;
            optimizedIndices = input.optimizedIndices;
            sampleMode = input.sampleMode;
            clipFrom = input.clipFrom;
            clipTo = input.clipTo;
        }

        public int GetClippedSampleCount(out int startIndex, out int endIndex)
        {
            startIndex = endIndex = 0;
            if (sampleMode == SplineComputer.SampleMode.Default)
            {
                startIndex = DMath.FloorInt((Count - 1) * clipFrom);
                endIndex = DMath.CeilInt((Count - 1) * clipTo);
            }
            else
            {
                double clipFromLerp = 0.0, clipToLerp = 0.0;
                GetSamplingValues(clipFrom, out startIndex, out clipFromLerp);
                GetSamplingValues(clipTo, out endIndex, out clipToLerp);
                if (clipToLerp > 0.0 && endIndex < Count - 1) endIndex++;
            }
            if (samplesAreLooped) //Handle looping segments
            {
                int toSamples = endIndex + 1;
                int fromSamples = Count - startIndex;
                return toSamples + fromSamples;
            }

            return endIndex - startIndex + 1;
        }

        /// <summary>
        /// Takes a regular 0-1 percent mapped to the start and end of the spline and maps it to the clipFrom and clipTo valies. Useful for working with clipped samples
        /// </summary>
        /// <param name="percent"></param>
        /// <returns></returns>
        public double ClipPercent(double percent)
        {
            ClipPercent(ref percent);
            return percent;
        }

        /// <summary>
        /// Takes a regular 0-1 percent mapped to the start and end of the spline and maps it to the clipFrom and clipTo valies. Useful for working with clipped samples
        /// </summary>
        /// <param name="percent"></param>
        /// <returns></returns>
        public void ClipPercent(ref double percent)
        {
            if (Count == 0)
            {
                percent = 0.0;
                return;
            }

            if (samplesAreLooped)
            {
                if (percent >= clipFrom && percent <= 1.0) { percent = DMath.InverseLerp(clipFrom, clipFrom + span, percent); }//If in the range clipFrom - 1.0
                else if (percent <= clipTo) { percent = DMath.InverseLerp(clipTo - span, clipTo, percent); } //if in the range 0.0 - clipTo
                else
                {
                    //Find the nearest clip start
                    if (DMath.InverseLerp(clipTo, clipFrom, percent) < 0.5) percent = 1.0;
                    else percent = 0.0;
                }
            }
            else percent = DMath.InverseLerp(clipFrom, clipTo, percent);
        }

        public double UnclipPercent(double percent)
        {
            UnclipPercent(ref percent);
            return percent;
        }

        public void UnclipPercent(ref double percent)
        {
            if(percent == 0.0)
            {
                percent = clipFrom;
                return;
            } else if(percent == 1.0)
            {
                percent = clipTo;
                return;
            }
            if (samplesAreLooped)
            {
                double fromLength = (1.0 - clipFrom) / span;
                if (fromLength == 0.0)
                {
                    percent = 0.0;
                    return;
                }
                if (percent < fromLength) percent = DMath.Lerp(clipFrom, 1.0, percent / fromLength);
                else if (clipTo == 0.0)
                {
                    percent = 0.0;
                    return;
                }
                else percent = DMath.Lerp(0.0, clipTo, (percent - fromLength) / (clipTo / span));
            }
            else percent = DMath.Lerp(clipFrom, clipTo, percent);
            percent = DMath.Clamp01(percent);
        }

        public void GetSamplingValues(double percent, out int sampleIndex, out double lerp)
        {
            lerp = 0.0;
            if (sampleMode == SplineComputer.SampleMode.Optimized)
            {
                double indexValue = percent * (optimizedIndices.Length - 1);
                int index = DMath.FloorInt(indexValue);
                sampleIndex = optimizedIndices[index];
                double lerpPercent = 0.0;
                if (index < optimizedIndices.Length - 1)
                {
                    //Percent 0-1 between the sampleIndex and the next sampleIndex
                    double indexLerp = indexValue - index;
                    double sampleIndexPercent = (double)index / (optimizedIndices.Length - 1);
                    double nextSampleIndexPercent = (double)(index + 1) / (optimizedIndices.Length - 1);
                    //Percent 0-1 of the sample between the sampleIndices' percents
                    lerpPercent = DMath.Lerp(sampleIndexPercent, nextSampleIndexPercent, indexLerp);
                }
                //Debug.Log(percent + " sample index " + index + " -> " + sampleIndex);
                if (sampleIndex < Count - 1) lerp = DMath.InverseLerp(samples[sampleIndex].percent, samples[sampleIndex + 1].percent, lerpPercent);
                return;
            }

            sampleIndex = DMath.FloorInt(percent * (Count - 1));
            lerp = (Count - 1) * percent - sampleIndex;
        }

        /// <summary>
        /// Same as Spline.EvaluatePosition but the result is transformed by the computer's transform
        /// </summary>
        /// <param name="percent">Evaluation percent</param>
        /// <param name="mode">Mode to use the method in. Cached uses the cached samples while Calculate is more accurate but heavier</param>
        /// <returns></returns>
        public Vector3 EvaluatePosition(double percent)
        {
            if (!hasSamples) return Vector3.zero;
            UnclipPercent(ref percent);
            int index;
            double lerp;
            GetSamplingValues(percent, out index, out lerp);
            if (lerp > 0.0) return Vector3.Lerp(samples[index].position, samples[index + 1].position, (float)lerp);
            else return samples[index].position;
        }

        /// <summary>
        /// Same as Spline.Evaluate but the result is transformed by the computer's transform
        /// </summary>
        /// <param name="percent">Evaluation percent</param>
        /// <param name="mode">Mode to use the method in. Cached uses the cached samples while Calculate is more accurate but heavier</param>
        /// <returns></returns>
        public SplineSample Evaluate(double percent)
        {
            SplineSample result = new SplineSample();
            Evaluate(percent, result);
            return result;
        }

        /// <summary>
        /// Same as Spline.Evaluate but the result is transformed by the computer's transform
        /// </summary>
        /// <param name="result"></param>
        /// <param name="percent"></param>
        public void Evaluate(double percent, SplineSample result)
        {
            if (!hasSamples)
            {
                result = new SplineSample();
                return;
            }
            UnclipPercent(ref percent);
            int index;
            double lerp;
            GetSamplingValues(percent, out index, out lerp);
            if (lerp > 0.0) SplineSample.Lerp(samples[index], samples[index + 1], lerp, result);
            else result.CopyFrom(samples[index]);
        }

        /// <summary>
        /// Same as Spline.Evaluate but the results are transformed by the computer's transform
        /// </summary>
        /// <param name="from">Start position [0-1]</param>
        /// <param name="to">Target position [from-1]</param>
        /// <returns></returns>
        public void Evaluate(ref SplineSample[] results, double from = 0.0, double to = 1.0)
        {
            if (!hasSamples)
            {
                results = new SplineSample[0];
                return;
            }
            Spline.FormatFromTo(ref from, ref to);
            int fromIndex, toIndex;
            double lerp;
            GetSamplingValues(from, out fromIndex, out lerp);
            GetSamplingValues(to, out toIndex, out lerp);
            if (lerp > 0.0 && toIndex < Count - 1) toIndex++;
            int clippedIterations = toIndex - fromIndex + 1;
            if (results == null) results = new SplineSample[clippedIterations];
            else if (results.Length != clippedIterations) results = new SplineSample[clippedIterations];
            results[0] = Evaluate(from);
            results[results.Length - 1] = Evaluate(to);
            for (int i = 1; i < results.Length - 1; i++) results[i] = samples[i + fromIndex];
        }

        /// <summary>
        /// Same as Spline.EvaluatePositions but the results are transformed by the computer's transform
        /// </summary>
        /// <param name="from">Start position [0-1]</param>
        /// <param name="to">Target position [from-1]</param>
        /// <returns></returns>
        public void EvaluatePositions(ref Vector3[] positions, double from = 0.0, double to = 1.0)
        {
            if (!hasSamples)
            {
                positions = new Vector3[0];
                return;
            }
            Spline.FormatFromTo(ref from, ref to);
            int fromIndex, toIndex;
            double lerp;
            GetSamplingValues(from, out fromIndex, out lerp);
            GetSamplingValues(to, out toIndex, out lerp);
            if (lerp > 0.0 && toIndex < Count - 1) toIndex++;
            int clippedIterations = toIndex - fromIndex + 1;
            if (positions == null) positions = new Vector3[clippedIterations];
            else if (positions.Length != clippedIterations) positions = new Vector3[clippedIterations];
            positions[0] = EvaluatePosition(from);
            positions[positions.Length - 1] = EvaluatePosition(to);
            for (int i = 1; i < positions.Length - 1; i++) positions[i] = samples[i + fromIndex].position;
        }

        /// <summary>
        /// Returns the percent from the spline at a given distance from the start point
        /// </summary>
        /// <param name="start">The start point</param>
        /// /// <param name="distance">The distance to travel</param>
        /// <param name="direction">The direction towards which to move</param>
        /// <returns></returns>
        public double Travel(double start, float distance, Spline.Direction direction, out float moved)
        {
            moved = 0f;
            if (!hasSamples) return 0.0;
            if (direction == Spline.Direction.Forward && start >= 1.0) return clipTo;
            else if (direction == Spline.Direction.Backward && start <= 0.0) return clipFrom;

            double lastPercent = UnclipPercent(DMath.Clamp01(start));
            if (distance == 0f) return lastPercent;
            Vector3 lastPos = EvaluatePosition(start);
            int sampleIndex;
            double lerp;
            GetSamplingValues(lastPercent, out sampleIndex, out lerp);
            if (direction == Spline.Direction.Forward && lerp > 0.0) sampleIndex++;
            float lastDistance = 0f;
            int minIndex = 0;
            int maxIndex = Count - 1;
            if (samplesAreLooped)
            {
                GetSamplingValues(clipFrom, out minIndex, out lerp);
                GetSamplingValues(clipTo, out maxIndex, out lerp);
                if (lerp > 0.0) maxIndex++;
            }
            while (moved < distance)
            {
                lastDistance = Vector3.Distance(samples[sampleIndex].position, lastPos);
                moved += lastDistance;
                if (moved >= distance) break;
                lastPos = samples[sampleIndex].position;
                lastPercent = samples[sampleIndex].percent;
                if (direction == Spline.Direction.Forward)
                {
                    if (sampleIndex == Count - 1)
                    {
                        if (samplesAreLooped)
                        {
                            lastPos = samples[0].position;
                            lastPercent = samples[0].percent;
                            sampleIndex = 1;
                        }
                        else break;
                    }
                    if (samplesAreLooped && sampleIndex == maxIndex) break;
                    sampleIndex++;
                }
                else
                {
                    if (sampleIndex == 0)
                    {
                        if (samplesAreLooped)
                        {
                            lastPos = samples[Count-1].position;
                            lastPercent = samples[Count - 1].percent;
                            sampleIndex = Count - 2;
                        }
                        else break;
                    }
                    if (samplesAreLooped && sampleIndex == minIndex) break;
                    sampleIndex--;
                }
            }
            float moveExcess = 0f;
            if (moved > distance) moveExcess = moved - distance;

            double p = DMath.Lerp(lastPercent, samples[sampleIndex].percent, 1f - moveExcess / lastDistance);
            moved -= moveExcess;
            return p;
        }

        public double Travel(double start, float distance, Spline.Direction direction = Spline.Direction.Forward)
        {
            float moved;
            return Travel(start, distance, direction, out moved);
        }

        /// <summary>
        /// Same as Spline.Project but the point is transformed by the computer's transform.
        /// </summary>
        /// <param name="position">Point in space</param>
        /// <param name="subdivide">Subdivisions default: 4</param>
        /// <param name="from">Sample from [0-1] default: 0f</param>
        /// <param name="to">Sample to [0-1] default: 1f</param>
        /// <param name="mode">Mode to use the method in. Cached uses the cached samples while Calculate is more accurate but heavier</param>
        /// <param name="subdivisions">Subdivisions for the Calculate mode. Don't assign if not using Calculated mode.</param>
        /// <returns></returns>
        public void Project(Vector3 position, int controlPointCount, SplineSample result, double from = 0.0, double to = 1.0)
        {
            if (!hasSamples) return;
            if (Count == 1)
            {
                if (result == null) result = new SplineSample(samples[0]);
                else result.CopyFrom(samples[0]);
                return;
            }
            Spline.FormatFromTo(ref from, ref to);
            //First make a very rough sample of the from-to region 
            int steps = (controlPointCount - 1) * 6; //Sampling six points per segment is enough to find the closest point range
            int step = Count / steps;
            if (step < 1) step = 1;
            float minDist = (position - samples[0].position).sqrMagnitude;
            int fromIndex = 0;
            int toIndex = Count - 1;
            double lerp;
            if (from != 0.0) GetSamplingValues(from, out fromIndex, out lerp);
            if (to != 1.0)
            {
                GetSamplingValues(to, out toIndex, out lerp);
                if (lerp > 0.0 && toIndex < Count - 1) toIndex++;
            }
            int checkFrom = fromIndex;
            int checkTo = toIndex;

            //Find the closest point range which will be checked in detail later
            for (int i = fromIndex; i <= toIndex; i += step)
            {
                if (i > toIndex) i = toIndex;
                float dist = (position - samples[i].position).sqrMagnitude;
                if (dist < minDist)
                {
                    minDist = dist;
                    checkFrom = Mathf.Max(i - step, 0);
                    checkTo = Mathf.Min(i + step, Count - 1);
                }
                if (i == toIndex) break;
            }
            minDist = (position - samples[checkFrom].position).sqrMagnitude;

            int index = checkFrom;
            //Find the closest result within the range
            for (int i = checkFrom + 1; i <= checkTo; i++)
            {
                float dist = (position - samples[i].position).sqrMagnitude;
                if (dist < minDist)
                {
                    minDist = dist;
                    index = i;
                }
            }
            //Project the point on the line between the two closest samples
            int backIndex = index - 1;
            if (backIndex < 0) backIndex = 0;
            int frontIndex = index + 1;
            if (frontIndex > Count - 1) frontIndex = Count - 1;
            Vector3 back = LinearAlgebraUtility.ProjectOnLine(samples[backIndex].position, samples[index].position, position);
            Vector3 front = LinearAlgebraUtility.ProjectOnLine(samples[index].position, samples[frontIndex].position, position);
            float backLength = (samples[index].position - samples[backIndex].position).magnitude;
            float frontLength = (samples[index].position - samples[frontIndex].position).magnitude;
            float backProjectDist = (back - samples[backIndex].position).magnitude;
            float frontProjectDist = (front - samples[frontIndex].position).magnitude;
            if (backIndex < index && index < frontIndex)
            {
                if ((position - back).sqrMagnitude < (position - front).sqrMagnitude)
                {
                    SplineSample.Lerp(samples[backIndex], samples[index], backProjectDist / backLength, result);
                    if (sampleMode == SplineComputer.SampleMode.Uniform) result.percent = DMath.Lerp(GetSamplePercent(backIndex), GetSamplePercent(index), backProjectDist / backLength);
                }
                else
                {
                    SplineSample.Lerp(samples[frontIndex], samples[index], frontProjectDist / frontLength, result);
                    if (sampleMode == SplineComputer.SampleMode.Uniform) result.percent = DMath.Lerp(GetSamplePercent(frontIndex), GetSamplePercent(index), frontProjectDist / frontLength);
                }
            }
            else if (backIndex < index)
            {
                SplineSample.Lerp(samples[backIndex], samples[index], backProjectDist / backLength, result);
                if (sampleMode == SplineComputer.SampleMode.Uniform) result.percent = DMath.Lerp(GetSamplePercent(backIndex), GetSamplePercent(index), backProjectDist / backLength);
            }
            else
            {
                SplineSample.Lerp(samples[frontIndex], samples[index], frontProjectDist / frontLength, result);
                if (sampleMode == SplineComputer.SampleMode.Uniform) result.percent = DMath.Lerp(GetSamplePercent(frontIndex), GetSamplePercent(index), frontProjectDist / frontLength);
            }

            if (Count > 1 && from == 0.0 && to == 1.0 && result.percent < samples[1].percent) //Handle looped splines
            {
                Vector3 projected = LinearAlgebraUtility.ProjectOnLine(samples[Count - 1].position, samples[Count - 2].position, position);
                if ((position - projected).sqrMagnitude < (position - result.position).sqrMagnitude)
                {
                    double l = LinearAlgebraUtility.InverseLerp(samples[Count - 1].position, samples[Count - 2].position, projected);
                    SplineSample.Lerp(samples[Count - 1], samples[Count - 2], l, result);
                    if (sampleMode == SplineComputer.SampleMode.Uniform) result.percent = DMath.Lerp(GetSamplePercent(Count - 1), GetSamplePercent(Count - 2), l);
                }
            } 
        }

        double GetSamplePercent(int sampleIndex)
        {
            if (sampleMode == SplineComputer.SampleMode.Optimized)
            {
                return samples[optimizedIndices[sampleIndex]].percent;
            }
            return (double)sampleIndex / (Count - 1);
        }

        /// <summary>
        /// Same as Spline.CalculateLength but this takes the computer's transform into account when calculating the length.
        /// </summary>
        /// <param name="from">Calculate from [0-1] default: 0f</param>
        /// <param name="to">Calculate to [0-1] default: 1f</param>
        /// <param name="resolution">Resolution [0-1] default: 1f</param>
        /// <param name="address">Node address of junctions</param>
        /// <returns></returns>
        public float CalculateLength(double from = 0.0, double to = 1.0)
        {
            if (!hasSamples) return 0f;
            Spline.FormatFromTo(ref from, ref to);
            float length = 0f;
            Vector3 pos = EvaluatePosition(from);
            int fromIndex, toIndex;
            double lerp;
            GetSamplingValues(from, out fromIndex, out lerp);
            GetSamplingValues(to, out toIndex, out lerp);
            if (lerp > 0.0 && toIndex < Count - 1) toIndex++;
            for (int i = fromIndex+1; i < toIndex; i++)
            {
                length += Vector3.Distance(samples[i].position, pos);
                pos = samples[i].position;
            }
            length += Vector3.Distance(EvaluatePosition(to), pos);
            return length;
        }

    }
}
