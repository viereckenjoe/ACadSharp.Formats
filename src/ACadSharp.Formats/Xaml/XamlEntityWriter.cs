using ACadSharp.Entities;
using ACadSharp.Extensions;
using ACadSharp.IO;
using ACadSharp.Tables;
using ACadSharp.Types.Units;
using CSMath;
using CSMath.Extensions;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;


namespace ACadSharp.Formats.Xaml;

internal class XamlEntityWriter
{
	public readonly List<(Entity, Transform)> _entities = new();

	private readonly CadXamlXmlWriter _writer;

	public XamlEntityWriter(CadXamlXmlWriter writer)
	{
		this._writer = writer;
	}

	public void AddEntity(Entity entity, Transform transform)
	{
		this._entities.Add((entity, transform));
	}

	public void WriteEntities()
	{
		foreach (var (entity, transform) in this._entities)
		{
			this.WriteEntity(entity, transform);
		}
	}

	public void WriteEntity(Entity entity, Transform transform)
	{
		switch (entity)
		{
			case Arc arc:
				this.writeArc(arc, transform);
				break;
			case Dimension dimension:
				this.writeDimension(dimension, transform);
				break;
			case Line line:
				this.writeLine(line, transform);
				break;
			case Point point:
				this.writePoint(point, transform);
				break;
			case Circle circle:
				this.writeCircle(circle, transform);
				break;
			case Ellipse ellipse:
				this.writeEllipse(ellipse, transform);
				break;
			case Hatch hatch:
				this.writeHatch(hatch, transform);
				break;
			case Insert insert:
				this.writeInsert(insert, transform);
				break;
			case IPolyline polyline:
				this.writePolyline(polyline, transform);
				break;
			case IText text:
				this.writeText(text, transform);
				break;
			//case Spline spline:
			//	this.writeSpline(spline, transform);
			//	break;
			case Solid solid:
				this.writeSolid(solid, transform);
				break;
			default:
				_writer.Notify($"[{entity.ObjectName}] Entity not implemented.", NotificationType.NotImplemented);
				break;
		}
	}

	public void WriteEntity(Entity entity)
	{
		this.WriteEntity(entity, new Transform());
	}


	//TODO: Change this to create a XamlPathGeometry string instead of a SvgPathGeometry string
	private string createPath(params IEnumerable<IPolyline> polylines)
	{
		StringBuilder sb = new StringBuilder();

		foreach (var item in polylines)
		{
			var pts = item.GetPoints<XY>().ToArray();
			if (!pts.Any())
			{
				continue;
			}

			var pt = pts[0];
			sb.Append($"M {this._writer.ToXamlPixelSizeFormat(pt)} ");
			for (int i = 1; i < pts.Length; i++)
			{
				pt = pts[i];
				sb.Append($"L {this._writer.ToXamlPixelSizeFormat(pt)} ");
			}

			if (item.IsClosed)
			{
				sb.Append("Z");
			}
		}

		return sb.ToString();
	}

	private bool drawableLineType(LineType lineType)
	{
		return lineType.IsComplex && !lineType.HasShapes;
	}

	private string xamlPoints<T>(IEnumerable<T> points, Transform transform)
		where T : IVector, new()
	{
		if (!points.Any())
		{
			return string.Empty;
		}

		StringBuilder sb = new StringBuilder();
		sb.Append(this._writer.ToXamlPixelSizeFormat(points.First()));
		foreach (T point in points.Skip(1))
		{
			sb.Append(' ');
			sb.Append(this._writer.ToXamlPixelSizeFormat(point));
		}

		return sb.ToString();
	}

	//TODO: Change this to write a XamlArcSegment instead of a SvgArc
	private void writeArc(Arc arc, Transform transform)
	{
		_writer.WriteStartElement("path");

		this.writeEntityHeader(arc, transform);

		//A rx ry rotation large-arc-flag sweep-flag x y

		arc.GetEndVertices(out XYZ start, out XYZ end);
		var largeArc = Math.Abs(arc.Sweep) > MathHelper.PI ? 1 : 0;

		StringBuilder sb = new StringBuilder();

		sb.Append($"M {this._writer.ToXamlPixelSizeFormat(start)}");
		sb.Append($" ");
		sb.Append($"A {this._writer.ToXamlPixelSizeFormat(arc.Radius)} {this._writer.ToXamlPixelSizeFormat(arc.Radius)}");
		sb.Append($" ");
		sb.Append($"{0} {largeArc} {1} {this._writer.ToXamlPixelSizeFormat(end)}");

		_writer.WriteAttributeString("d", sb.ToString());

		_writer.WriteAttributeString("fill", "none");

		_writer.WriteEndElement();
	}

	//TODO: Change this to write a XamlCircle instead of a SvgCircle
	private void writeCircle(Circle circle, Transform transform)
	{
		var loc = transform.ApplyTransform(circle.Center);

		_writer.WriteStartElement("circle");

		this.writeEntityHeader(circle, transform);

		_writer.WriteAttributeString("r", circle.Radius);
		_writer.WriteAttributeString("cx", loc.X);
		_writer.WriteAttributeString("cy", loc.Y);

		_writer.WriteAttributeString("fill", "none");

		_writer.WriteEndElement();
	}

	//TODO: Change this to write XamlDashes instead of SvgDashes 
	private void writeDashes(IEnumerable<double> dashes)
	{
		StringBuilder sb = new StringBuilder();

		foreach (var d in dashes)
		{
			sb.Append(this._writer.ToXamlPixelSizeFormat(Math.Abs(d)));
			sb.Append(' ');
		}

		_writer.WriteAttributeString("stroke-dasharray", sb.ToString().Trim());
	}

	//TODO: Change this to write XamlDashes instead of SvgDashes
	private void writeDashes(LineType lineType, double pointSize)
	{
		StringBuilder sb = new StringBuilder();
		foreach (LineType.Segment segment in lineType.Segments)
		{
			if (segment.IsPoint)
			{
				sb.Append(this._writer.ToXamlPixelSizeFormat(pointSize));
			}
			else
			{
				sb.Append(this._writer.ToXamlPixelSizeFormat(Math.Abs(segment.Length)));
			}

			sb.Append(' ');
		}

		_writer.WriteAttributeString("stroke-dasharray", sb.ToString().Trim());
	}

	//TODO: Change this to write a XamlDimension instead of a SvgDimension
	private void writeDimension(Dimension dimension, Transform transform)
	{
		_writer.WriteStartElement("g");

		if (dimension.Block == null)
		{
			dimension.UpdateBlock();
		}

		foreach (Entity e in dimension.Block.Entities)
		{
			this.WriteEntity(e, transform);
		}

		_writer.WriteEndElement();
	}

	//TODO: Change this to write a XamlEllipse instead of a SvgEllipse
	private void writeEllipse(Ellipse ellipse, Transform transform)
	{
		if (ellipse.IsFullEllipse)
		{
			_writer.WriteStartElement("path");

			this.writeEntityHeader(ellipse, transform);

			StringBuilder sb = new StringBuilder();

			XYZ start = ellipse.PolarCoordinateRelativeToCenter(0);
			XYZ end = ellipse.PolarCoordinateRelativeToCenter(Math.PI);

			string rx = this._writer.ToXamlPixelSizeFormat(ellipse.MajorAxis / 2);
			string ry = this._writer.ToXamlPixelSizeFormat(ellipse.MinorAxis / 2);
			string x_axis_rotation = MathHelper.RadToDeg(ellipse.Rotation).ToXaml();
			string xy = this._writer.ToXamlPixelSizeFormat(end);

			sb.Append($"M {this._writer.ToXamlPixelSizeFormat(start)} ");
			sb.Append($"A {rx} {ry} {x_axis_rotation} {0} {1} {xy} ");

			start = ellipse.PolarCoordinateRelativeToCenter(Math.PI);
			end = ellipse.PolarCoordinateRelativeToCenter(MathHelper.TwoPI);

			xy = this._writer.ToXamlPixelSizeFormat(end);
			sb.Append($"A {rx} {ry} {x_axis_rotation} {0} {1} {xy}");

			//A rx ry rotation large-arc-flag sweep-flag x y
			_writer.WriteAttributeString("d", sb.ToString());

			_writer.WriteAttributeString("fill", "none");
			_writer.WriteEndElement();
		}
		else
		{
			_writer.WriteStartElement("polyline");

			this.writeEntityHeader(ellipse, transform);

			var vertices = ellipse.PolygonalVertexes(256);
			string pts = this.xamlPoints(vertices, transform);
			_writer.WriteAttributeString("points", pts);
			_writer.WriteAttributeString("fill", "none");

			_writer.WriteEndElement();

			return;

			//TODO: Fix the ellipse generation
			_writer.WriteStartElement("path");

			this.writeEntityHeader(ellipse, transform);

			ellipse.GetEndVertices(out XYZ start, out XYZ end);

			//A rx ry rotation large-arc-flag sweep-flag x y
			_writer.WriteAttributeString("d", $"M {start.ToPixelSize(this._writer.Units).ToXaml()} A {ellipse.MajorAxis} {ellipse.MinorAxis} {MathHelper.RadToDeg(ellipse.Rotation)} {0} {1} {end.ToPixelSize(this._writer.Units).ToXaml()}");

			_writer.WriteAttributeString("fill", "none");
			_writer.WriteEndElement();
		}
	}

	//TODO: Change this to write an Entity as a XamlPath instead of a SvgPath
	private void writeEntityAsPath<T>(Entity entity, Transform transform, params IEnumerable<T> points)
		where T : IVector
	{
		//Will be needed to write the linetypes that use shapes
		double pointSize = this._writer.GetPointSize(entity);
		var lines = entity.GetActiveLineType().CreateLineTypeShape(pointSize, points);

		_writer.WriteStartElement("path");

		this.writeEntityHeader(entity, transform);

		_writer.WriteAttributeString("d", this.createPath(lines));

		_writer.WriteEndElement();
	}

	//TODO: Change this to write a XamlHeader instead of a SvgHeader
	private void writeEntityHeader(IEntity entity, Transform transform, bool drawStroke = true)
	{
		Color color = entity.GetActiveColor();

		//_writer.WriteAttributeString("id", entity.Handle.ToString());

		//this._writer.WriteStartAttribute("class");
		//this._writer.WriteValue("entity");
		//this._writer.WriteValue(" ");
		//this._writer.WriteValue($"layer_{entity.Layer.Name}");
		//this._writer.WriteEndAttribute();

		//StringBuilder style = new StringBuilder();

		if (!drawStroke)
		{
			//style.Append($"stroke:none;");
		}
		else if (!entity.Color.IsByLayer)
		{
			//style.Append($"stroke:{this._writer.ColorXaml(color)};");
			_writer.WriteAttributeString("Stroke", this._writer.ColorXaml(color));
		}
		else
		{			
			_writer.WriteAttributeString("Stroke", this._writer.ColorXaml(entity.Layer.Color));
		}

		var lineWeight = entity.GetActiveLineWeightType();
		
		if (entity.LineWeight != LineWeightType.ByLayer)
		{
			_writer.WriteAttributeString("StrokeThickness", this._writer.LineWeightValueToXaml(lineWeight));
		}
		else
		{
			_writer.WriteAttributeString("StrokeThickness", this._writer.LineWeightValueToXaml(entity.Layer.LineWeight));
		}


		this.writeTransform(transform);

		LineType lt = entity.GetActiveLineType();
		if (this.drawableLineType(lt))
		{
			this.writeDashes(lt, this._writer.GetPointSize(entity));
		}
	}

	//TODO: Change this to write a XamlHatch instead of a SvgHatch
	private void writeHatch(Hatch hatch, Transform transform)
	{
		_writer.WriteStartElement("g");

		var patternId = this.writePattern(hatch);

		List<Polyline3D> plines = new List<Polyline3D>();
		foreach (Hatch.BoundaryPath path in hatch.Paths)
		{
			var pline = new Polyline3D(path.GetPoints(this._writer.Configuration.ArcPoints));
			plines.Add(pline);
		}

		_writer.WriteStartElement("path");

		this.writeEntityHeader(hatch, transform, drawStroke: false);

		_writer.WriteAttributeString("d", this.createPath(plines));

		_writer.WriteAttributeString("fill", $"url(#{patternId})");

		_writer.WriteEndElement();

		_writer.WriteEndElement();
	}

	private void writeInsert(Insert insert, Transform transform)
	{
		var insertTransform = insert.GetTransform();
		var merged = new Transform(transform.Matrix * insertTransform.Matrix);

		_writer.WriteStartElement("g");
		this.writeTransform(merged);

		foreach (var e in insert.Block.Entities)
		{
			this.WriteEntity(e);
		}

		_writer.WriteEndElement();
	}

	//TODO: Change this to write a XamlLine instead of a SvgLine
	private void writeLine(Line line, Transform transform)
	{
		_writer.WriteStartElement("Line");

		this.writeEntityHeader(line, transform);

		_writer.WriteAttributeString("X1", this._writer.ToXamlFormat(line.StartPoint.X));
		_writer.WriteAttributeString("Y1", this._writer.ToXamlFormat(line.StartPoint.Y));
		_writer.WriteAttributeString("X2", this._writer.ToXamlFormat(line.EndPoint.X));
		_writer.WriteAttributeString("Y2", this._writer.ToXamlFormat(line.EndPoint.Y));

		_writer.WriteEndElement();
	}

	//TODO: Change this to write a XamlPattern string instead of a SvgPattern string
	private string writePattern(Hatch hatch)
	{
		if (hatch.IsSolid)
		{
			return this.writeSolidPattern(hatch);
		}

		Dictionary<string, BoundingBox> patterns = new();
		foreach (var item in hatch.Pattern.Lines)
		{
			var i = $"{item.GetHashCode()}_line";
			patterns.Add(i, new BoundingBox(XYZ.Zero, new XYZ(item.LineOffset, item.LineOffset, 0)));

			//Each line works individually repeating itself every offset
			this.writePatternHeader(i);
			_writer.WriteAttributeString("width", this._writer.ToXamlFormat(item.LineOffset));
			_writer.WriteAttributeString("height", this._writer.ToXamlFormat(item.LineOffset));

			this.writeTransform(name: "patternTransform",
				translation: item.BasePoint.Convert<XYZ>().ToPixelSize(this._writer.Units));
			//rotation: -item.Angle);

			_writer.WriteStartElement("line");

			//Direction of the line
			var length = item.Offset.GetLength();
			double x = MathHelper.Cos(item.Angle) * 10;
			double y = MathHelper.Sin(item.Angle) * 10;

			//Offset -> is the size of the line box

			//Add BasePoint
			_writer.WriteAttributeString("x1", 0.0d.ToXaml(this._writer.Units));
			_writer.WriteAttributeString("y1", 0.0d.ToXaml(this._writer.Units));
			_writer.WriteAttributeString("x2", (x).ToXaml(this._writer.Units));
			_writer.WriteAttributeString("y2", (y).ToXaml(this._writer.Units));

			//Rotate the pattern after line
			//_writer.WriteAttributeString("x1", 0.0d.ToSvg(this._writer.Units));
			//_writer.WriteAttributeString("y1", (item.LineOffset / 2).ToSvg(this._writer.Units));
			//_writer.WriteAttributeString("x2", 1.0d.ToSvg(this._writer.Units));
			//_writer.WriteAttributeString("y2", (item.LineOffset / 2).ToSvg(this._writer.Units));

			_writer.WriteAttributeString("stroke", this._writer.ColorXaml(hatch.GetActiveColor()));
			_writer.WriteAttributeString("stroke-width", $"{this._writer.LineWeightValueToXaml(hatch.GetActiveLineWeightType())}");

			if (item.DashLengths.Any())
			{
				this.writeDashes(item.DashLengths);
			}

			//Line
			_writer.WriteEndElement();

			if (false)
			{
				_writer.WriteStartElement("rect");
				_writer.WriteAttributeString("width", (item.LineOffset).ToXaml(this._writer.Units));
				_writer.WriteAttributeString("height", (item.LineOffset).ToXaml(this._writer.Units));
				_writer.WriteAttributeString("fill", $"none");
				_writer.WriteAttributeString("stroke", $"red");
				_writer.WriteEndElement();
			}

			//Pattern
			_writer.WriteEndElement();
		}

		string id = this.writePatternHeader(hatch);
		var width = patterns.Values.Max(w => w.LengthX);
		var height = patterns.Values.Max(w => w.LengthY);

		_writer.WriteAttributeString("width", width.ToXaml(this._writer.Units));
		_writer.WriteAttributeString("height", height.ToXaml(this._writer.Units));

		foreach (var item in patterns)
		{
			_writer.WriteStartElement("rect");
			_writer.WriteAttributeString("width", (item.Value.LengthX).ToXaml(this._writer.Units));
			_writer.WriteAttributeString("height", (item.Value.LengthY).ToXaml(this._writer.Units));
			_writer.WriteAttributeString("fill", $"url(#{item.Key})");
			_writer.WriteEndElement();
		}

		//pattern
		_writer.WriteEndElement();

		return id;
	}

	//TODO: Change this to write a XamlPatternHeader instead of a SvgPatternHeader
	private void writePatternHeader(string id)
	{
		_writer.WriteStartElement("pattern");

		_writer.WriteAttributeString("id", id);
		_writer.WriteAttributeString("patternUnits", "userSpaceOnUse");
	}

	//TODO: Change this to write a XamlPatternHeader string instead of a SvgPatternHeader string
	private string writePatternHeader(Hatch hatch)
	{
		string id = $"{hatch.Pattern.GetHashCode()}_{hatch.Pattern.Name}";

		_writer.WriteStartElement("pattern");

		_writer.WriteAttributeString("id", id);
		_writer.WriteAttributeString("patternUnits", "userSpaceOnUse");

		return id;
	}

	//TODO: Chamge this to write a XamlPoint instead of a SvgPoint
	private void writePoint(Point point, Transform transform)
	{
		_writer.WriteStartElement("circle");

		this.writeEntityHeader(point, transform);

		_writer.WriteAttributeString("r", this._writer.Configuration.PointRadius, UnitsType.Unitless);
		_writer.WriteAttributeString("cx", point.Location.X);
		_writer.WriteAttributeString("cy", point.Location.Y);

		_writer.WriteAttributeString("fill", this._writer.ColorXaml(point.GetActiveColor()));

		_writer.WriteEndElement();
	}

	//TODO: Change this to write a XamlPolyline instead of a SvgPolyline
	private void writePolyline(IPolyline polyline, Transform transform)
	{
		if (polyline.IsClosed)
		{
			_writer.WriteStartElement("polygon");
		}
		else
		{
			_writer.WriteStartElement("polyline");
		}

		this.writeEntityHeader(polyline, transform);

		string pts = this.xamlPoints(polyline.GetPoints<XY>(this._writer.Configuration.ArcPoints), transform);

		_writer.WriteAttributeString("points", pts);
		_writer.WriteAttributeString("fill", "none");

		_writer.WriteEndElement();
	}

	//TODO: Change this to write a XamlSolid instead of a SvgSolid
	private void writeSolid(Solid solid, Transform transform)
	{
		_writer.WriteStartElement("polygon");

		this.writeEntityHeader(solid, transform);

		string pts = this.xamlPoints([solid.FirstCorner, solid.SecondCorner, solid.ThirdCorner, solid.FourthCorner], transform);
		_writer.WriteAttributeString("points", pts);
		_writer.WriteAttributeString("fill", this._writer.ColorXaml(solid.GetActiveColor()));

		_writer.WriteEndElement();
	}

	//TODO: Change this to write a XamlSolidPattern string instead of a SvgSolidPattern string
	private string writeSolidPattern(Hatch hatch)
	{
		string id = this.writePatternHeader(hatch);

		_writer.WriteAttributeString("width", "100%");
		_writer.WriteAttributeString("height", "100%");

		_writer.WriteStartElement("rect");

		_writer.WriteAttributeString("width", "100%");
		_writer.WriteAttributeString("height", "100%");
		_writer.WriteAttributeString("fill", this._writer.ColorXaml(hatch.Color));

		//rect
		_writer.WriteEndElement();

		//pattern
		_writer.WriteEndElement();

		return id;
	}

	//TODO: Change this to write a XamlSpline instead of a SvgSpline
	private void writeSpline(Spline spline, Transform transform)
	{
		spline.UpdateFromFitPoints();
		this.writeEntityAsPath(spline, transform, spline.PolygonalVertexes(this._writer.Configuration.ArcPoints));
	}

	//TODO: Change this to write a XamlText instead of a SvgText
	private void writeText(IText text, Transform transform)
	{
		XYZ insert;

		if (text is TextEntity lineText
			&& (lineText.HorizontalAlignment != TextHorizontalAlignment.Left
			|| lineText.VerticalAlignment != TextVerticalAlignmentType.Baseline)
			&& !(lineText.HorizontalAlignment == TextHorizontalAlignment.Fit
			|| lineText.HorizontalAlignment == TextHorizontalAlignment.Aligned))
		{
			insert = lineText.AlignmentPoint;
		}
		else
		{
			insert = text.InsertPoint;
		}

		_writer.WriteStartElement("g");
		this.writeTransform(transform);

		_writer.WriteStartElement("text");

		this.writeTransform(translation: insert.ToPixelSize(this._writer.Units), scale: new XYZ(1, -1, 0), rotation: text.Rotation != 0 ? text.Rotation : null);

		_writer.WriteAttributeString("fill", this._writer.ColorXaml(text.GetActiveColor()));

		//<text x="20" y="35" class="small">My</text>	
		this._writer.WriteStartAttribute("style");
		this._writer.WriteValue("font:");
		this._writer.WriteValue(text.Height.ToXaml(this._writer.Units));
		if (this._writer.Units == UnitsType.Unitless)
		{
			this._writer.WriteValue("px");
		}

		if (text.Style.TrueType.HasFlag(FontFlags.Bold))
		{
			this._writer.WriteValue("bold");
		}

		if (text.Style.TrueType.HasFlag(FontFlags.Italic))
		{
			this._writer.WriteValue("italic");
		}

		this._writer.WriteValue(" ");
		this._writer.WriteValue(Path.GetFileNameWithoutExtension(text.Style.Filename));
		this._writer.WriteEndAttribute();

		switch (text)
		{
			case MText mtext:
				switch (mtext.AttachmentPoint)
				{
					case AttachmentPointType.TopLeft:
						_writer.WriteAttributeString("alignment-baseline", "hanging");
						_writer.WriteAttributeString("text-anchor", "start");
						break;
					case AttachmentPointType.TopCenter:
						_writer.WriteAttributeString("alignment-baseline", "hanging");
						_writer.WriteAttributeString("text-anchor", "middle");
						break;
					case AttachmentPointType.TopRight:
						_writer.WriteAttributeString("alignment-baseline", "hanging");
						_writer.WriteAttributeString("text-anchor", "end");
						break;
					case AttachmentPointType.MiddleLeft:
						_writer.WriteAttributeString("alignment-baseline", "middle");
						_writer.WriteAttributeString("text-anchor", "start");
						break;
					case AttachmentPointType.MiddleCenter:
						_writer.WriteAttributeString("alignment-baseline", "middle");
						_writer.WriteAttributeString("text-anchor", "middle");
						break;
					case AttachmentPointType.MiddleRight:
						_writer.WriteAttributeString("alignment-baseline", "middle");
						_writer.WriteAttributeString("text-anchor", "end");
						break;
					case AttachmentPointType.BottomLeft:
						_writer.WriteAttributeString("alignment-baseline", "baseline");
						_writer.WriteAttributeString("text-anchor", "start");
						break;
					case AttachmentPointType.BottomCenter:
						_writer.WriteAttributeString("alignment-baseline", "baseline");
						_writer.WriteAttributeString("text-anchor", "middle");
						break;
					case AttachmentPointType.BottomRight:
						_writer.WriteAttributeString("alignment-baseline", "baseline");
						_writer.WriteAttributeString("text-anchor", "end");
						break;
					default:
						break;
				}

				foreach (var item in mtext.GetPlainTextLines())
				{
					_writer.WriteStartElement("tspan");
					_writer.WriteAttributeString("x", 0);
					_writer.WriteAttributeString("dy", "1em");
					this._writer.WriteString(item);
					_writer.WriteEndElement();
				}

				//Line to avoid the strange offset at the end
				_writer.WriteStartElement("tspan");
				_writer.WriteAttributeString("x", 0);
				_writer.WriteAttributeString("dy", "1em");
				_writer.WriteAttributeString("visibility", "hidden");
				this._writer.WriteString(".");
				_writer.WriteEndElement();
				break;
			case TextEntity textEntity:

				switch (textEntity.HorizontalAlignment)
				{
					case TextHorizontalAlignment.Left:
						_writer.WriteAttributeString("text-anchor", "start");
						break;
					case TextHorizontalAlignment.Middle:
					case TextHorizontalAlignment.Center:
						_writer.WriteAttributeString("text-anchor", "middle");
						break;
					case TextHorizontalAlignment.Right:
						_writer.WriteAttributeString("text-anchor", "end");
						break;
				}

				switch (textEntity.VerticalAlignment)
				{
					case TextVerticalAlignmentType.Baseline:
					case TextVerticalAlignmentType.Bottom:
						_writer.WriteAttributeString("alignment-baseline", "baseline");
						break;
					case TextVerticalAlignmentType.Middle:
						_writer.WriteAttributeString("alignment-baseline", "middle");
						break;
					case TextVerticalAlignmentType.Top:
						_writer.WriteAttributeString("alignment-baseline", "hanging");
						break;
				}

				this._writer.WriteString(text.Value);
				break;
		}

		_writer.WriteEndElement();
		_writer.WriteEndElement();
	}

	//TODO: Change this to write a XamlTransform instead of a SvgTransform
	private void writeTransform(Transform transform)
	{
		XYZ? translation = transform.Translation != XYZ.Zero ? transform.Translation : null;
		XYZ? scale = transform.Scale != new XYZ(1) ? transform.Scale : null;
		double? rotation = transform.EulerRotation.Z != 0 ? transform.EulerRotation.Z : null;

		this.writeTransform(translation: translation, scale: scale, rotation: rotation);
	}

	//TODO: Change this to write a XamlTransform instead of a SvgTransform
	private void writeTransform(string name = "RenderTransform", XYZ? translation = null, XYZ? scale = null, double? rotation = null)
	{
		StringBuilder sb = new StringBuilder();


		if (translation.HasValue)
		{
			var t = translation.Value;

			sb.Append($"translate(");
			sb.Append($"{t.X.ToString(CultureInfo.InvariantCulture)},");
			sb.Append($"{t.Y.ToString(CultureInfo.InvariantCulture)})");
		}

		if (scale.HasValue)
		{
			var s = scale.Value;

			sb.Append($"scale(");
			sb.Append($"{s.X.ToString(CultureInfo.InvariantCulture)},");
			sb.Append($"{s.Y.ToString(CultureInfo.InvariantCulture)})");
		}

		if (rotation.HasValue)
		{
			var r = -MathHelper.RadToDeg(rotation.Value);

			sb.Append($"rotate(");
			sb.Append($"{r.ToString(CultureInfo.InvariantCulture)})");
		}

		if (string.IsNullOrEmpty(sb.ToString()))
		{
			return;
		}

		_writer.WriteAttributeString(name, sb.ToString());
	}

}
