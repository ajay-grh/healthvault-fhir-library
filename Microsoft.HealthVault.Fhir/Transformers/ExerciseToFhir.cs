﻿// Copyright (c) Microsoft Corporation.  All rights reserved.
// MIT License
// Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the ""Software""), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED *AS IS*, WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

using System.Collections.Generic;
using Hl7.Fhir.Model;
using Hl7.Fhir.Support;
using Microsoft.HealthVault.Fhir.Codings;
using Microsoft.HealthVault.Fhir.Constants;
using Microsoft.HealthVault.ItemTypes;

namespace Microsoft.HealthVault.Fhir.Transformers
{
    public static partial class ThingBaseToFhir
    {
        // Register the type on the generic ThingToFhir partial class
        public static Observation ToFhir(this Exercise exercise)
        {
            return ExerciseToFhir.ToFhirInternal(exercise, ToFhirInternal(exercise));
        }
    }

    /// <summary>
    /// An extension class that transforms HealthVault exercise data types into FHIR Observations
    /// </summary>
    internal static class ExerciseToFhir
    {
        internal static Observation ToFhirInternal(Exercise exercise, Observation observation)
        {
            observation.Category = new List<CodeableConcept>() { FhirCategories.VitalSigns };
            observation.Code = new CodeableConcept(VocabularyUris.HealthVaultVocabulariesUri, HealthVaultVocabularies.Exercise);

            if (exercise.Distance != null)
            {
                var distanceValue = new Observation.ComponentComponent
                {
                    Code = new CodeableConcept { Coding = new List<Coding> { new Coding(VocabularyUris.HealthVaultVocabulariesUri, HealthVaultVocabularies.ExerciseDistance) } },
                    Value = new Quantity((decimal)exercise.Distance.Value, "m") //TODO: switch to constant once body composition PR (#15) is in
                };
                observation.Component.Add(distanceValue);
            }

            if (exercise.Duration != null)
            {
                var durationValue = new Observation.ComponentComponent
                {
                    Code = new CodeableConcept { Coding = new List<Coding> { new Coding(VocabularyUris.HealthVaultVocabulariesUri, HealthVaultVocabularies.ExerciseDuration) } },
                    Value = new Quantity((decimal)exercise.Duration.Value, "min") //TODO: switch to constant once body composition PR (#15) is in
                };
                observation.Component.Add(durationValue);
            }

            observation.Text = new Narrative
            {
                Div = exercise.Title
            };

            // ToDo: Change to use the ApproximateDateTime.ToFhir() extension when brought in with body composition pr (#15).
            observation.Effective = new FhirDateTime(
                exercise.When.ApproximateDate.Year,
                exercise.When.ApproximateDate.Month ?? 1,
                exercise.When.ApproximateDate.Day ?? 1,
                exercise.When.ApproximateTime.Hour,
                exercise.When.ApproximateTime.Minute,
                exercise.When.ApproximateTime.Second ?? 0
            );
            
            var activityValue = new Observation.ComponentComponent
            {
                Code = new CodeableConcept { Coding = new List<Coding> { new Coding(VocabularyUris.HealthVaultVocabulariesUri, HealthVaultVocabularies.ExerciseActivity) } },
                Value = new CodeableConcept { Coding = HealthVaultCodesToFhir.ConvertCodableValueToFhir(exercise.Activity, null) }
            };
            observation.Component.Add(activityValue);

            if (exercise.Details != null)
            {
                foreach (var detail in exercise.Details)
                {
                    observation.Extension.Add(CreateDetailExtension(detail.Key, detail.Value));
                }
            }

            if (!exercise.Segments.IsNullOrEmpty())
            {
                foreach (var segment in exercise.Segments)
                {
                    observation.Extension.Add(CreateSegmentExtension(segment));
                }
            }

            return observation;
        }

        private static Extension CreateDetailExtension(string key, ExerciseDetail exerciseDetail)
        {
            var extension = new Extension
            {
                Url = HealthVaultVocabularies.BaseUri + HealthVaultVocabularies.ExerciseDetail
            };

            extension.Extension.Add(new Extension("exercise-detail-name", new FhirString(key)));

            extension.Extension.Add(new Extension("exercise-detail-type", new CodeableConcept{Coding = HealthVaultCodesToFhir.ConvertCodableValueToFhir(new CodableValue(exerciseDetail.Name.Value, exerciseDetail.Name), null) }));

            extension.Extension.Add(new Extension("exercise-detail-value", new Quantity
            {
                Value = (decimal)exerciseDetail.Value.Value,
                Unit = exerciseDetail.Value.Units[0].Value
            }));
          
            return extension;
        }

        private static Extension CreateSegmentExtension(ExerciseSegment segment)
        {
            var extension = new Extension
            {
                Url = HealthVaultVocabularies.BaseUri + HealthVaultVocabularies.ExerciseSegment
            };
            
            extension.Extension.Add(new Extension("exercise-segment-activity", new CodeableConcept { Coding = HealthVaultCodesToFhir.ConvertCodableValueToFhir(segment.Activity, null) }));

            if (!string.IsNullOrEmpty(segment.Title))
            {
                extension.Extension.Add(new Extension("exercise-segment-title", new FhirString(segment.Title)));
            }

            if (segment.Duration.HasValue)
            { 
                extension.Extension.Add(new Extension("exercise-segment-duration", new FhirDecimal((decimal)segment.Duration)));
            }

            if (segment.Distance != null)
            {
                extension.Extension.Add(new Extension("exercise-segment-distance", new Quantity
                {
                    Value = (decimal)segment.Distance.Value,
                    Unit = "m" //TODO: switch to constant once body composition PR (#15) is in
                }));
            }

            if (segment.Offset.HasValue)
            {
                extension.Extension.Add(new Extension("exercise-segment-offset", new FhirDecimal((decimal)segment.Offset)));
            }

            if (segment.Details != null)
            {
                foreach (var detail in segment.Details)
                {
                    extension.Extension.Add(CreateDetailExtension(detail.Key, detail.Value));
                }
            }

            return extension;
        }
    }
}