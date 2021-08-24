using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace UnityEngine.Perception.GroundTruth.Exporters
{
    public interface IDatasetExporter
    {
        string GetRgbCaptureFilename(params(string, object)[] additionalSensorValues);

        void OnSimulationBegin(string directoryName);
        void OnSimulationEnd();

        void OnAnnotationRegistered<TSpec>(Guid annotationId, TSpec[] values);

        void OnMetricRegistered(Guid metricId, string name, string description);

        Task ProcessPendingCaptures(List<SimulationState.PendingCapture> pendingCaptures, SimulationState simState);

        Task ProcessPendingMetrics(List<SimulationState.PendingMetric> pendingMetrics, SimulationState simState);

        Task OnCaptureReported(Guid sequence, int step, int width, int height, string filename, Vector3 position, Quaternion rotation, Vector3 velocity, Vector3 acceleration);
    }
}
