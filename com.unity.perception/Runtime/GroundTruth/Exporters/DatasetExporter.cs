using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace UnityEngine.Perception.GroundTruth.Exporters
{
    public abstract class DatasetExporter : MonoBehaviour
    {
        public void Reset()
        {
            SimulationState.RegisterExporter(this);
        }

        public void OnEnable()
        {
            SimulationState.RegisterExporter(this);
        }

        public void OnDisable()
        {
            SimulationState.DeregisterExporter(this);
        }

        public abstract string GetName();

        public abstract void OnSimulationBegin(string directoryName);
        public abstract void OnSimulationEnd();

        public abstract void OnAnnotationRegistered<TSpec>(Guid annotationId, TSpec[] values);

        public abstract Task ProcessPendingCaptures(List<SimulationState.PendingCapture> pendingCaptures, SimulationState simState);

        public abstract Task OnCaptureReported(int frame, int width, int height, string filename);
    }
}
