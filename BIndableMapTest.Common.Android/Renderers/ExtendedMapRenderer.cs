using System;
using Xamarin.Forms;
using BindableMapTest.Controls;
using BindableMapTest.Android.Renderers;
using Xamarin.Forms.Maps.Android;
using Android.Gms.Maps;
using Android.Gms.Maps.Model;
using Xamarin.Forms.Maps;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using Android.App;
using Android.Content;
using Android.Content.Res;
using Android.Graphics;
using Android.OS;
using Android.Provider;
using BindableMapTest.Interfaces;
using BIndableMapTest.Common.Android;
using Xamarin;
using Color = Xamarin.Forms.Color;

[assembly: ExportRenderer (typeof(ExtendedMap), typeof(ExtendedMapRenderer))]
namespace BindableMapTest.Android.Renderers
{
	public class ExtendedMapRenderer : MapRenderer
	{
		bool _isDrawnDone;
		private static int _pinResource;

		public static void Init(Activity activity, Bundle bundle, int pinResource)
		{
			_pinResource = pinResource;
			FormsMaps.Init(activity, bundle);
		}

		protected override void OnElementChanged(Xamarin.Forms.Platform.Android.ElementChangedEventArgs<View> e)
		{
			base.OnElementChanged(e);
			var formsMap = (ExtendedMap)Element;
			var androidMapView = (MapView)Control;

			if (androidMapView != null && androidMapView.Map != null)
			{
				androidMapView.Map.InfoWindowClick += MapOnInfoWindowClick;
			}

			if (formsMap != null)
			{
				((ObservableCollection<Pin>)formsMap.Pins).CollectionChanged += OnCollectionChanged;
			}
		}

		protected override void OnElementPropertyChanged (object sender, PropertyChangedEventArgs e)
		{
			base.OnElementPropertyChanged (sender, e);

			if (e.PropertyName.Equals ("VisibleRegion") && !_isDrawnDone) {
				UpdatePins();

				_isDrawnDone = true;

			}
		}

		void OnCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
		{
			UpdatePins();
		}

		private void UpdatePins()
		{
			var androidMapView = (MapView)Control;
			var formsMap = (ExtendedMap)Element;

			androidMapView.Map.Clear ();

			androidMapView.Map.MarkerClick += HandleMarkerClick;
			androidMapView.Map.MyLocationEnabled = formsMap.IsShowingUser;

			var items = formsMap.Items;

			foreach (var item in items) {
				var markerWithIcon = new MarkerOptions ();
				markerWithIcon.SetPosition (new LatLng (item.Location.Latitude, item.Location.Longitude));
				markerWithIcon.SetTitle (string.IsNullOrWhiteSpace(item.Name) ? "-" : item.Name);
				markerWithIcon.SetSnippet (item.Details);

				try
				{
					//var bitmapDescriptor = BitmapDescriptorFactory.FromResource(GetPinIcon());
					var bitmapDescriptor = BitmapDescriptorFactory.FromBitmap(GetBitmapMarker(Context, _pinResource, "5m"));
					markerWithIcon.InvokeIcon(bitmapDescriptor);
				}
				catch (Exception)
				{
					markerWithIcon.InvokeIcon(BitmapDescriptorFactory.DefaultMarker());
				}

				androidMapView.Map.AddMarker (markerWithIcon);
			}
		}

		public Bitmap GetBitmapMarker(Context mContext, int resourceId, string text)
		{
			Resources resources = mContext.Resources;
			float scale = resources.DisplayMetrics.Density;
			Bitmap bitmap = BitmapFactory.DecodeResource(resources, resourceId);

			Bitmap.Config bitmapConfig = bitmap.GetConfig();

			// set default bitmap config if none
			if (bitmapConfig == null)
				bitmapConfig = Bitmap.Config.Argb8888;

			bitmap = bitmap.Copy(bitmapConfig, true);

			Canvas canvas = new Canvas(bitmap);
			Paint paint = new Paint(PaintFlags.AntiAlias);
			paint.Color = global::Android.Graphics.Color.Black;
			paint.TextSize = ((int)(14 * scale));
			paint.SetShadowLayer(1f, 0f, 1f, global::Android.Graphics.Color.White);

			// draw text to the Canvas center
			Rect bounds = new Rect();
			paint.GetTextBounds(text, 0, text.Length, bounds);
			int x = (bitmap.Width - bounds.Width()) / 2;
			int y = (bitmap.Height + bounds.Height()) / 2 - 20;

			canvas.DrawText(text, x, y, paint);

			return bitmap;
		}

		private static int GetPinIcon()
		{
			return _pinResource;
		}

		private void HandleMarkerClick (object sender, GoogleMap.MarkerClickEventArgs e)
		{
			var marker = e.Marker;
			marker.ShowInfoWindow ();

			var map = this.Element as ExtendedMap;

			var formsPin = new ExtendedPin(marker.Title,marker.Snippet, marker.Position.Latitude, marker.Position.Longitude);

			map.SelectedPin = formsPin;
		}

		private void MapOnInfoWindowClick (object sender, GoogleMap.InfoWindowClickEventArgs e)
		{
			Marker clickedMarker = e.Marker;
			// Find the matchin item
			var formsMap = (ExtendedMap)Element;
			formsMap.ShowDetailCommand.Execute(formsMap.SelectedPin);
		}

		private bool IsItem(IMapModel item, Marker marker)
		{
			return item.Name == marker.Title && 
				   item.Details == marker.Snippet && 
				   item.Location.Latitude == marker.Position.Latitude && 
				   item.Location.Longitude == marker.Position.Longitude;
		}

		protected override void OnLayout (bool changed, int l, int t, int r, int b)
		{
			base.OnLayout (changed, l, t, r, b);

			//NOTIFY CHANGE

			if (changed) {
				_isDrawnDone = false;
			}
		} 
	}
}


