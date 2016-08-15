using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Effects;

namespace Loci.Effects
{
    public class GrayscaleEffect : ShaderEffect
    {
        private static readonly PixelShader GrayScalePixelShader= new PixelShader()
        {
            UriSource = new Uri("pack://application:,,,/Resources/grayscaleeffect.ps")
        };
        public static readonly DependencyProperty InputProperty = RegisterPixelShaderSamplerProperty("Input", typeof(GrayscaleEffect), 0);
        public static readonly DependencyProperty DesaturationFactorProperty = DependencyProperty.Register("DesaturationFactor", typeof(double), typeof(GrayscaleEffect), new UIPropertyMetadata(0.0, PixelShaderConstantCallback(0), CoerceDesaturationFactor));

        public Brush Input
        {
            get
            {
                return (Brush)GetValue(InputProperty);
            }
            set
            {
                SetValue(InputProperty, value);
            }
        }

        public double DesaturationFactor
        {
            get
            {
                return (double)GetValue(DesaturationFactorProperty);
            }
            set
            {
                SetValue(DesaturationFactorProperty, value);
            }
        }

        public GrayscaleEffect()
        {
            PixelShader = GrayScalePixelShader;
            UpdateShaderValue(InputProperty);
            UpdateShaderValue(DesaturationFactorProperty);
        }

        private static object CoerceDesaturationFactor(DependencyObject d, object value)
        {
            var grayscaleEffect = (GrayscaleEffect)d;
            var num = (double)value;
            if (num < 0.0 || num > 1.0)
                return grayscaleEffect.DesaturationFactor;
            return num;
        }
    }
}
