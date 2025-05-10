Overview:

FontParser: creates FontData
FontData: contains info about font -- in particular the Glyph array
Glyph: class containing contours, bounds, etc. for a particular glyph

# Types shared between C# and HLSL
InstanceData : position, bounds, etc of each glyph. Also a dataIndex for indexing into GlyphMetaData
GlyphMetaData (represented as integer array) : bezier data start index, num contours, contour length/s
BezierData (represented as float2 array) : bezier points
