using ACadSharp.Types.Units;
using CSMath;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ACadSharp.Formats.Xaml
{
	internal static class XamlConverter
	{
		public static double ToPixelSize(this double value, UnitsType units)
		{
			switch (units)
			{
				case UnitsType.Inches:
					return value * 96;
				case UnitsType.Millimeters:
					return value * 96 / 25.4;
				case UnitsType.Unitless:
					return value;
				default:
					throw new InvalidEnumArgumentException(nameof(units), (int)units, units.GetType());
			}
		}

		public static T ToPixelSize<T>(this T value, UnitsType units)
			where T : IVector
		{
			for (int i = 0; i < value.Dimension; i++)
			{
				value[i] = ToPixelSize(value[i], units);
			}

			return value;
		}

		public static string ToXaml(this double value, int? decimalPlaces = null)
		{
			if (decimalPlaces != null)
			{
				return value.ToString($"N{decimalPlaces}", CultureInfo.InvariantCulture);
			}
			else
			{
				return value.ToString(CultureInfo.InvariantCulture);
			}
		}

		public static string ToXaml(this double value, UnitsType units, int? decimalPlaces = null)
		{
			string unitSufix = string.Empty;
			switch (units)
			{
				case UnitsType.Centimeters:
					unitSufix = "cm";
					break;
				case UnitsType.Millimeters:
					unitSufix = "mm";
					break;
				case UnitsType.Inches:
					unitSufix = "in";
					break;
			}

			//return $"{value.ToXaml(decimalPlaces)}{unitSufix}";
			return $"{value.ToXaml(decimalPlaces)}";

		}

		public static string ToXaml<T>(this T vector, int? decimalPlaces = null)
			where T : IVector
		{
			return $"{vector[0].ToXaml(decimalPlaces)},{vector[1].ToXaml(decimalPlaces)}";
		}

		public static string ToXaml<T>(this T vector, UnitsType units, int? decimalPlaces = null)
			where T : IVector
		{
			return $"{vector[0].ToXaml(units, decimalPlaces)},{vector[1].ToXaml(units, decimalPlaces)}";
		}
	}
}
