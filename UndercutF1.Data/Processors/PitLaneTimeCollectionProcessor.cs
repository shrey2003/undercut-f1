namespace UndercutF1.Data;

public class PitLaneTimeCollectionProcessor() : IProcessor<PitLaneTimeCollectionDataPoint>
{
    public PitLaneTimeCollectionDataPoint Latest { get; private set; } = new();

    public void Process(PitLaneTimeCollectionDataPoint data)
    {
        foreach (var (driverNumber, pitTime) in data.PitTimes)
        {
            if (Latest.PitTimesList.TryGetValue(driverNumber, out var existing))
            {
                existing.Add(pitTime);
            }
            else
            {
                Latest.PitTimesList.Add(driverNumber, [pitTime]);
            }

            Latest.PitTimes[driverNumber] = pitTime;
        }
    }
}
