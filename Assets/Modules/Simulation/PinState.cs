namespace DLS.Simulation
{
	public enum PinState { FLOATING, LOW, HIGH }

	public static class PinStateExtensions
	{
		public static int ToInt(this PinState state)
		{
			return state == PinState.HIGH ? 1 : 0;
		}
	}
}