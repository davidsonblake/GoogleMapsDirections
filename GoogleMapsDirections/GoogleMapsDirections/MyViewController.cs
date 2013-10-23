using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;
using Google.Maps;
using MonoTouch.CoreLocation;
using MonoTouch.UIKit;
using System.Drawing;
using System.Net;
using Newtonsoft.Json;

namespace GoogleMapsDirections
{
    public class MyViewController : UIViewController
    {

        MapView _mapView;
        private MapDelegate _mapDelegate;

        public override void ViewDidLoad()
        {
            base.ViewDidLoad();

            View.Frame = UIScreen.MainScreen.Bounds;
            View.BackgroundColor = UIColor.White;
            View.AutoresizingMask = UIViewAutoresizing.FlexibleWidth | UIViewAutoresizing.FlexibleHeight;

            SetupMap();

            //Setup button to clear map
            var btn = new UIButton(new RectangleF(0, 0, 100, 50));
            btn.Layer.BorderWidth = 1;
            btn.Layer.CornerRadius = 5;
            btn.Layer.BorderColor = UIColor.Black.CGColor;
            btn.SetTitle(@"Clear Map", UIControlState.Normal);
            btn.BackgroundColor = UIColor.Gray;
            btn.TouchUpInside += (s, e) =>
                {
                    _mapView.Clear();
                    _mapDelegate.Lines.Clear();
                    _mapDelegate.Locations.Clear();
                };


            _mapView.AddSubview(btn);
        }

        private void SetupMap()
        {

            //Init Map wiht Camera
            var camera = new CameraPosition(new CLLocationCoordinate2D(36.069082, -94.155976), 15, 30, 0);
            _mapView = MapView.FromCamera(RectangleF.Empty, camera);
            _mapView.MyLocationEnabled = true;

            //Add button to zoom to location
            _mapView.Settings.MyLocationButton = true;

            _mapView.MyLocationEnabled = true;
            _mapView.MapType = MapViewType.Hybrid;
            _mapView.Settings.SetAllGesturesEnabled(true);

            //Init MapDelegate
            _mapDelegate = new MapDelegate(_mapView);
            _mapView.Delegate = _mapDelegate;

            View = _mapView;
        }


        public class MapDelegate : MapViewDelegate
        {
            //Base URL for Directions Service
            const string KMdDirectionsUrl = @"http://maps.googleapis.com/maps/api/directions/json?origin=";

            public readonly List<CLLocationCoordinate2D> Locations;
            private readonly MapView _map;
            public readonly List<Google.Maps.Polyline> Lines;

            public MapDelegate(MapView map)
            {
                Locations = new List<CLLocationCoordinate2D>();
                Lines = new List<Google.Maps.Polyline>();
                _map = map;
            }

            public override void DidTapAtCoordinate(MapView mapView, CLLocationCoordinate2D coordinate)
            {

                //Create/Add Marker 
                var marker = new Marker { Position = coordinate, Map = mapView };
                Locations.Add(coordinate);

                if (Locations.Count > 1)
                {
                    SetDirectionsQuery();
                }
            }

            private async void SetDirectionsQuery()
            {
                //Clear Old Polylines
                if (Lines.Count > 0)
                {
                    foreach (var line in Lines)
                    {
                        line.Map = null;
                    }
                    Lines.Clear();
                }

                //Start building Directions URL
                var sb = new System.Text.StringBuilder();
                sb.Append(KMdDirectionsUrl);
                sb.Append(Locations[0].Latitude.ToString(CultureInfo.InvariantCulture));
                sb.Append(",");
                sb.Append(Locations[0].Longitude.ToString(CultureInfo.InvariantCulture));
                sb.Append("&");
                sb.Append("destination=");
                sb.Append(Locations[1].Latitude.ToString(CultureInfo.InvariantCulture));
                sb.Append(",");
                sb.Append(Locations[1].Longitude.ToString(CultureInfo.InvariantCulture));
                sb.Append("&sensor=true");

                //If we have more than 2 locations we'll append waypoints
                if (Locations.Count > 2)
                {
                    sb.Append("&waypoints=");
                    for (var i = 2; i < Locations.Count; i++)
                    {
                        if (i > 2)
                            sb.Append("|");
                        sb.Append(Locations[i].Latitude.ToString(CultureInfo.InvariantCulture));
                        sb.Append(",");
                        sb.Append(Locations[i].Longitude.ToString(CultureInfo.InvariantCulture));
                    }
                }

                //Get directions through Google Web Service
                var directionsTask = GetDirections(sb.ToString());

                var jSonData = await directionsTask;

                //Deserialize string to object
                var routes = JsonConvert.DeserializeObject<RootObject>(jSonData);

                foreach (var route in routes.routes)
                {
                    //Encode path from polyline passed back
                    var path = Path.FromEncodedPath(route.overview_polyline.points);

                    //Create line from Path
                    var line = Google.Maps.Polyline.FromPath(path);
                    line.StrokeWidth = 10f;
                    line.StrokeColor = UIColor.Red;
                    line.Geodesic = true;

                    //Place line on map
                    line.Map = _map;
                    Lines.Add(line);

                }

            }

            private async Task<String> GetDirections(string url)
            {
                var client = new WebClient();
                var directionsTask = client.DownloadStringTaskAsync(url);
                var directions = await directionsTask;

                return directions;

            }

        }

        public override void ViewWillAppear(bool animated)
        {
            base.ViewWillAppear(animated);
            _mapView.StartRendering();
        }

        public override void ViewWillDisappear(bool animated)
        {
            base.ViewWillDisappear(animated);
            _mapView.StopRendering();
        }

    }
}

