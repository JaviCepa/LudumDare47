﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;

// Shapes © Freya Holmér - https://twitter.com/FreyaHolmer/
// Website & Documentation - https://acegikmo.com/shapes/
namespace Shapes {

	public static class CodegenDrawOverloads {

		public static void GenerateDrawOverloadsScript() {
			List<string> lines = new List<string>();
			lines.Add( "using UnityEngine;" );
			lines.Add( "using TMPro;" );
			lines.Add( "" );
			lines.Add( "// Shapes © Freya Holmér - https://twitter.com/FreyaHolmer/" );
			lines.Add( "// Website & Documentation - https://acegikmo.com/shapes/" );
			lines.Add( "namespace " + nameof(Shapes) + " {" );
			lines.Add( "" );
			lines.Add( "\t// this file is auto-generated by " + nameof(CodegenDrawOverloads) );
			lines.Add( "\tpublic static partial class " + nameof(Draw) + " {" );
			lines.AddRange( GenerateAllOverloads() );
			lines.Add( "\t}" );
			lines.Add( "" );
			lines.Add( "}" );
			string path = ShapesIO.RootFolder + "/Scripts/Runtime/Immediate Mode/DrawOverloads.cs";
			File.WriteAllLines( path, lines );
			AssetDatabase.Refresh(); // reload scripts
		}

		static List<string> GenerateAllOverloads() {
			List<string> lines = new List<string>();

			// find core draw functions as targets of the overloads
			Dictionary<string, TargetMethodCall> callTargets = typeof(Draw)
				.GetMethods( BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic )
				.Where( prop => Attribute.IsDefined( prop, typeof(OvldGenCallTarget) ) )
				.Select( m => new TargetMethodCall( m ) )
				.ToDictionary( m => m.name, m => m );

			// shared things
			OverloadGenerator g;
			OrSelectorParams rotIdentityNormalQuat = new OrSelectorParams(
				null,
				new[] { new Param( "Vector3 normal", "rot" ) { methodCallStr = "Quaternion.LookRotation( normal )" } },
				new Param[] { "Quaternion rot" }
			);

			// Line
			foreach( ( bool dashed, string suffix ) in new[] { ( false, "" ), ( true, "Dashed" ) } ) {
				g = new OverloadGenerator( "Line" + suffix, callTargets["Line"] );
				if( dashed == false )
					g.constAssigns["dashSize"] = "0f"; // make sure it's always 0
				g += "Vector3 start";
				g += "Vector3 end";
				if( dashed )
					g += new OrSelectorParams( null, new Param[] { "float dashSize", "float dashOffset" } );
				g += new CombinatorialParams( "float thickness", "LineEndCap endCaps" );
				g += new OrSelectorParams(
					null,
					new[] { new Param( "Color color", "colorStart", "colorEnd" ) },
					new Param[] { "Color colorStart", "Color colorEnd" }
				);

				g.GenerateAndAppend( lines );
			}

			// Polyline
			g = new OverloadGenerator( "Polyline", callTargets["Polyline"] );
			g += "PolylinePath path";
			g += new CombinatorialParams( "bool closed", "float thickness", "PolylineJoins joins", "Color color" );
			g.GenerateAndAppend( lines );

			// Disc / Ring / Pie / Arc
			string[] discTypeNames = { "Disc", "Ring", "Pie", "Arc" };
			string[] gradientTypeSuffix = { "", "GradientRadial", "GradientAngular", "GradientBilinear" };
			for( int idt = 0; idt < 4; idt++ ) {
				string discType = discTypeNames[idt];
				bool hollow = idt == 1 || idt == 3; // ring or arc
				bool angles = idt == 2 || idt == 3;
				for( int igr = 0; igr < 4; igr++ ) {
					g = new OverloadGenerator( discType + gradientTypeSuffix[igr], callTargets[discType] );
					g += "Vector3 pos";
					g += rotIdentityNormalQuat;
					if( hollow ) {
						// we have to do this because we can't use both radius and thickness on their own as overloads
						g += new OrSelectorParams(
							null,
							new[] { new Param( "float radius" ) },
							new[] { new Param( "float radius" ), new Param( "float thickness" ) }
						);
					} else {
						g += new CombinatorialParams( "float radius" );
					}

					if( angles ) {
						g += "float angleRadStart";
						g += "float angleRadEnd";
					}

					if( angles && hollow ) // arc only
						g += new OrSelectorParams( null, new[] { new Param( nameof(ArcEndCap) + " endCaps" ) } );

					if( igr == 0 ) { // single color, optional
						g += new CombinatorialParams( new[] { new Param( "Color color", "colorInnerStart", "colorOuterStart", "colorInnerEnd", "colorOuterEnd" ) } );
					} else if( igr == 1 ) { // radial gradient
						g += new Param( "Color colorInner", "colorInnerStart", "colorInnerEnd" );
						g += new Param( "Color colorOuter", "colorOuterStart", "colorOuterEnd" );
					} else if( igr == 2 ) { // angular gradient
						g += new Param( "Color colorStart", "colorInnerStart", "colorOuterStart" );
						g += new Param( "Color colorEnd", "colorInnerEnd", "colorOuterEnd" );
					} else if( igr == 3 ) { // bilinear
						g += "Color colorInnerStart";
						g += "Color colorOuterStart";
						g += "Color colorInnerEnd";
						g += "Color colorOuterEnd";
					}


					g.GenerateAndAppend( lines );
				}
			}

			// Rectangle / RectangleBorder
			foreach( ( bool bordered, string suffixOvld ) in new[] { ( false, "" ), ( true, "Border" ) } ) {
				for( int mode = 0; mode < 4; mode++ ) {
					bool doRect = mode == 0 || mode == 2;
					bool posRot = mode == 0 || mode == 1 || mode == 3;
					bool sizePivotParams = mode == 1 || mode == 3;
					bool noPivot = mode == 1; // mode 1 is without optional pivot param
					
					g = new OverloadGenerator( "Rectangle" + suffixOvld, callTargets["Rectangle"] );
					if( bordered )
						g.constAssigns["hollow"] = "true"; // make sure it's always true when hollow
					if( posRot ) {
						g += "Vector3 pos";
						g += rotIdentityNormalQuat;
					}

					if( doRect )
						g += "Rect rect";
					if( sizePivotParams ) {
						string pivot = noPivot ? "RectPivot.Center" : "pivot";
						g += new OrSelectorParams( new[] {
							new[] { new Param( "Vector2 size", "rect" ) { methodCallStr = $"{pivot}.GetRect( size )" } },
							new[] {
								new Param( "float width", "rect" ) { methodCallStr = $"{pivot}.GetRect( width, height )" },
								new Param( "float height" ) // a bit of a hack but it's fiiiine, this shouuld go ignored by the target call
							}
						} );
						if( noPivot == false )
							g += new Param( "RectPivot pivot" );
					}

					if( bordered )
						g += "float thickness";
					g += new OrSelectorParams( new[] {
						null,
						new[] { new Param( "float cornerRadius", "cornerRadii" ) { methodCallStr = "new Vector4( cornerRadius, cornerRadius, cornerRadius, cornerRadius )" } },
						new[] { new Param( "Vector4 cornerRadii" ) },
					} );
					g += new CombinatorialParams( "Color color" );
					g.GenerateAndAppend( lines );
				}
			}

			// Triangle
			g = new OverloadGenerator( "Triangle", callTargets["Triangle"] );
			g += "Vector3 a";
			g += "Vector3 b";
			g += "Vector3 c";
			g += new OrSelectorParams(
				null,
				new[] { new Param( "Color color", "colorA", "colorB", "colorC" ) },
				new Param[] { "Color colorA", "Color colorB", "Color colorC" }
			);
			g.GenerateAndAppend( lines );

			// Quad
			g = new OverloadGenerator( "Quad", callTargets["Quad"] );
			g += "Vector3 a";
			g += "Vector3 b";
			g += "Vector3 c";
			g += new CombinatorialParams( "Vector3 d" );
			g += new OrSelectorParams(
				null,
				new[] { new Param( "Color color", "colorA", "colorB", "colorC", "colorD" ) },
				new Param[] { "Color colorA", "Color colorB", "Color colorC", "Color colorD" }
			);
			g.GenerateAndAppend( lines );

			// Sphere
			g = new OverloadGenerator( "Sphere", callTargets["Sphere"] );
			g += "Vector3 pos";
			g += new CombinatorialParams( "float radius", "Color color" );
			g.GenerateAndAppend( lines );

			// Cuboid / Cube
			foreach( ( bool cube, string name ) in new[] { ( false, "Cuboid" ), ( true, "Cube" ) } ) {
				g = new OverloadGenerator( name, callTargets["Cuboid"] );
				g += "Vector3 pos";
				g += rotIdentityNormalQuat;
				if( cube )
					g += new Param( "float size" ) { methodCallStr = "new Vector3( size, size, size )" };
				else
					g += "Vector3 size";

				g += new CombinatorialParams( "Color color" );
				g.GenerateAndAppend( lines );
			}

			// Cone
			g = new OverloadGenerator( "Cone", callTargets["Cone"] );
			g += "Vector3 pos";
			g += rotIdentityNormalQuat;
			g += "float radius";
			g += "float length";
			g += new CombinatorialParams( "bool fillCap", "Color color" );
			g.GenerateAndAppend( lines );

			// Torus
			g = new OverloadGenerator( "Torus", callTargets["Torus"] );
			g += "Vector3 pos";
			g += rotIdentityNormalQuat;
			g += "float radius";
			g += "float thickness";
			g += new CombinatorialParams( "Color color" );
			g.GenerateAndAppend( lines );

			// Text
			g = new OverloadGenerator( "Text", callTargets["Text"] );
			g += "Vector3 pos";
			g += new OrSelectorParams(
				null,
				new[] { new Param( "Vector3 normal", "rot" ) { methodCallStr = "Quaternion.LookRotation( normal )" } },
				new Param[] { "Quaternion rot" },
				new[] { new Param( "float angle", "rot" ) { methodCallStr = "Quaternion.Euler( 0f, 0f, angle * Mathf.Rad2Deg )" } }
			);
			g += "string content";
			g += new CombinatorialParams( "TextAlign align", "float fontSize", "TMP_FontAsset font", "Color color" );
			g.GenerateAndAppend( lines );

			return lines;
		}

	}

}