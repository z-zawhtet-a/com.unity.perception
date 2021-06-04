using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace UnityEngine.Perception.GroundTruth.Exporters
{
    public interface IDatasetExporter
    {
        public void OnSimulationBegin(string directoryName);
        public void OnSimulationEnd();

        public void OnAnnotationRegistered<TSpec>(Guid annotationId, TSpec[] values);

        public Task ProcessPendingCaptures(List<SimulationState.PendingCapture> pendingCaptures, SimulationState simState);

        public Task OnCaptureReported(int frame, int width, int height, string filename);
    }
}
