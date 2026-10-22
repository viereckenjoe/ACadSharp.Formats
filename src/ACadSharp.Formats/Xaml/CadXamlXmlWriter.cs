using ACadSharp.Entities;
using ACadSharp.Extensions;
using ACadSharp.Formats.Svg;
using ACadSharp.IO;
using ACadSharp.Types.Units;
using CSMath;
using System;
using System.IO;
using System.Text;
using System.Xml;

namespace ACadSharp.Formats.Xaml
{
	internal class CadXamlXmlWriter : XmlTextWriter
	{
		public event NotificationEventHandler OnNotification;
		public XamlConfiguration Configuration { get; } = new();
		public bool IsPaperSpace { get; set; } = false;
		public UnitsType Units { get; }

		public CadXamlXmlWriter(XamlConfiguration configuration, UnitsType units, Encoding encoding)
			: this(new MemoryStream(), configuration, units, encoding) { }

		public CadXamlXmlWriter(Stream stream, XamlConfiguration configuration, UnitsType units, Encoding encoding)
			: base(stream, encoding)
		{
			this.Configuration = configuration;
			this.Units = units;
			this.Formatting = configuration.Formatting;
		}

		public string ColorXaml(Color color)
		{
			if (this.IsPaperSpace && color.Equals(Color.Default))
			{
				color = Color.Black;
			}

			return $"#FF{color.R}{color.G}{color.B}";
		}

		public string ToXamlPixelSizeFormat(IVector value)
		{
			return value.ToPixelSize(this.Units).ToXaml(this.Configuration.DecimalPlaces);
		}

		public string ToXamlPixelSizeFormat(double value)
		{
			return value.ToPixelSize(this.Units).ToXaml(this.Configuration.DecimalPlaces);
		}

		public double GetPointSize(IEntity entity)
		{
			return entity.GetActiveLineWeightType().GetLineWeightValue().ToPixelSize(this.Units);
		}

		public string LineWeightValueToXaml(LineWeightType lineWeight)
		{
			return $"{this.Configuration.GetLineWeightValue(lineWeight, this.Units).ToXaml(UnitsType.Millimeters)}";
		}

		public void Notify(string message, NotificationType type, Exception ex = null)
		{
			this.triggerNotification(this, new NotificationEventArgs(message, type, ex));
		}

		public string ToXamlFormat(double value)
		{
			return value.ToXaml(this.Units, this.Configuration.DecimalPlaces);
		}

		protected void triggerNotification(object sender, NotificationEventArgs e)
		{
			this.OnNotification?.Invoke(sender, e);
		}

		public void WriteAttributeString(string localName, double value, UnitsType units)
		{
			this.WriteAttributeString(localName, value.ToSvg(units));
		}

		public void WriteAttributeString(string localName, double value)
		{
			this.WriteAttributeString(localName, value, this.Units);
		}
	}
}
