namespace Seb.Vis
{
	public enum Anchor
	{
		Centre, // x: centre   y: centre
		CentreLeft, // x: left     y: centre
		CentreRight, // x: right    y: centre
		CentreTop, // x: centre   y: top   
		CentreBottom,
		TopLeft,
		TopRight,
		BottomLeft,
		BottomRight,

		// Anchor text at centre left of first line. The vertical centre of the line
		// does not depend on the actual contents of the text here, which avoids the
		// positioning changing slightly based on the heights of the particular characters used.
		TextCentreLeft,
		TextCentreRight,
		TextFirstLineCentre,
		TextCentre
	}
}