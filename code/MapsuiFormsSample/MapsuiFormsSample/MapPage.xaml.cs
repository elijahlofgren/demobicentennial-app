﻿using System.Diagnostics;
using Mapsui.Geometries;
using Mapsui.Layers;
using Mapsui.Providers;
using Mapsui.Styles;
using Mapsui.Utilities;
using Mapsui.Projection;
using MapsuiFormsSample.TestData;
using MapsuiFormsSample.DataObjects;
using System.Collections.Generic;
using Newtonsoft.Json;
using Microsoft.CSharp.RuntimeBinder;
using System;
using System.Threading.Tasks;
using Plugin.Permissions;
using Plugin.Permissions.Abstractions;
#if __MOBILE__
using Plugin.Geolocator;
using Plugin.Geolocator.Abstractions;
#endif

namespace MapsuiFormsSample
{
    public partial class MapPage
    {
        private MapsUIView _mapControl = null;
        private List<Marker> _markersList = new List<Marker>();

        public MapPage()
        {
            this.Title = "Map";
            InitializeComponent();

            _mapControl = new MapsUIView();
            _mapControl.NativeMap.Layers.Add(OpenStreetMap.CreateTileLayer());

            _mapControl.NativeMap.Layers.Add(CreateLayer());

            // Set the center of the viewport to the coordinate. The UI will refresh automatically
            // mapControl.NativeMap.NavigateTo(sphericalMercatorCoordinate);
            // Additionally you might want to set the resolution, this could depend on your specific purpose
            // mapControl.NativeMap.NavigateTo(mapControl.NativeMap.Resolutions[18]);

            _mapControl.NativeMap.Info += (sender, args) =>
            {
                var layername = args.Layer?.Name;
                var featureLabel = args.Feature?["Label"]?.ToString();
                var featureType = args.Feature?["Type"]?.ToString();

                Debug.WriteLine("Info Event was invoked.");
                Debug.WriteLine("Layername: " + layername);
                Debug.WriteLine("Feature Label: " + featureLabel);
                Debug.WriteLine("Feature Type: " + featureType);

                Debug.WriteLine("World Postion: {0:F4} , {1:F4}", args.WorldPosition?.X, args.WorldPosition?.Y);
                Debug.WriteLine("Screen Postion: {0:F4} , {1:F4}", args.ScreenPosition?.X, args.ScreenPosition?.Y);
                ShowNearestMarker(args.WorldPosition);
            };

            ContentGrid.Children.Add(_mapControl);

            if (IsLocationAvailable())
            {
#if __MOBILE__
                GetCurrentLocation();

            } else
            {
                RequestLocationPermission();
            }
#endif
        }
#if __MOBILE__
        // Begin adapted from https://github.com/jamesmontemagno/permissionsplugin
        public async void RequestLocationPermission()
        {
            try
            {
                var status = await CrossPermissions.Current.CheckPermissionStatusAsync(Permission.Location);
                if (status != PermissionStatus.Granted)
                {
                    if (await CrossPermissions.Current.ShouldShowRequestPermissionRationaleAsync(Permission.Location))
                    {
                        await DisplayAlert("Need location", "Gunna need that location", "OK");
                    }

                    var results = await CrossPermissions.Current.RequestPermissionsAsync(Permission.Location);
                    //Best practice to always check that the key exists
                    if (results.ContainsKey(Permission.Location))
                        status = results[Permission.Location];
                }

                if (status == PermissionStatus.Granted)
                {
                    var results = await CrossGeolocator.Current.GetPositionAsync(TimeSpan.FromSeconds(20));
                    Debug.WriteLine("Lat: " + results.Latitude + " Long: " + results.Longitude);
                }
                else if (status != PermissionStatus.Unknown)
                {
                    await DisplayAlert("Location Denied", "Can not continue, try again.", "OK");
                }
            }
            catch (Exception ex)
            {

                Debug.WriteLine("Error: " + ex);
            }
        }
        // End adapted from https://github.com/jamesmontemagno/permissionsplugin
#endif

#if __MOBILE__
        // Begin from https://jamesmontemagno.github.io/GeolocatorPlugin/CurrentLocation.html
        public async Task<Position> GetCurrentLocation()
        {
            Position position = null;
            try
            {
                var locator = CrossGeolocator.Current;
                locator.DesiredAccuracy = 100;

                position = await locator.GetLastKnownLocationAsync();

                if (position != null)
                {
                    Debug.WriteLine("TMP DEBUG: CACHED Position: Lat: " + position.Latitude + " Long: " + position.Longitude);
                    return position;
                }

                if (!locator.IsGeolocationAvailable || !locator.IsGeolocationEnabled)
                {
                    //not available or enabled
                    Debug.WriteLine("Location not available or enabled.");
                    return null;
                }

                position = await locator.GetPositionAsync(TimeSpan.FromSeconds(20), null, true);
                Debug.WriteLine("TMP DEBUG: Uncached Position: Lat: " + position.Latitude + " Long: " + position.Longitude);
                return position;
            }
            catch (Exception ex)
            {
                //Display error as we have timed out or can't get location.
                Debug.WriteLine("Error getting user's current location: " + ex);
                return null;
            }
            
        }
        // End from https://jamesmontemagno.github.io/GeolocatorPlugin/CurrentLocation.html
#endif

        public bool IsLocationAvailable()
        {
#if __MOBILE__
                if (!CrossGeolocator.IsSupported)
                    return false;

                return CrossGeolocator.Current.IsGeolocationAvailable;
#else
                return false;
#endif
        }

        private async void ShowNearestMarker(Point worldPosition)
        {
            Debug.WriteLine("Viewport.Resolution: " + _mapControl.NativeMap.Viewport.Resolution);

            // Wait until zoomed into a certain amount.
            if (_mapControl.NativeMap.Viewport.Resolution < 10)
            {
                Tuple<double, Marker> closestMarkerDist = new Tuple<double, Marker>(Double.MaxValue, new Marker("", "", new Point(), ""));
                foreach (Marker marker in _markersList)
                {
                    double distance = worldPosition.Distance(marker.LocationSphericalMercator);
                    if (distance < closestMarkerDist.Item1)
                    {
                        closestMarkerDist = new Tuple<double, Marker>(distance, marker);
                    }
                }
                Debug.WriteLine("Closest Marker:");
                Debug.WriteLine("distance: " + closestMarkerDist.Item1);
                Debug.WriteLine("Title: " + closestMarkerDist.Item2.Title);

                if (closestMarkerDist.Item1 < 150)
                {
                    await Navigation.PushAsync(new MarkerInfoPage(closestMarkerDist.Item2));
                }
            }
        }

        public ILayer CreateLayer()
        {
            var memoryProvider = new MemoryProvider();

            string markersJson = TestMarkerData.JsonTestData;

            dynamic markers = JsonConvert.DeserializeObject(markersJson);
            foreach (dynamic marker in markers)
            {
                if ("historic_marker".Equals(marker.type.ToString()))
                {
                    string label = marker.title.ToString();

                    try
                    {
                        double lat = marker.field_coordinates.und[0].safe_value;
                        double longitude = marker.field_coordinates.und[1].safe_value;
                        // Get the lon lat coordinates from somewhere (Mapsui can not help you there)
                        // Format (Long, Lat)
                        // Zoom to marker location
                        var currentMarker = new Mapsui.Geometries.Point(longitude, lat);
                        // OSM uses spherical mercator coordinates. So transform the lon lat coordinates to spherical mercator
                        Point sphericalMercatorCoordinate = SphericalMercator.FromLonLat(currentMarker.X, currentMarker.Y);
                        string description = marker.field_description.und[0].safe_value.ToString();
                        _markersList.Add(new Marker(label, marker.nid.ToString(), sphericalMercatorCoordinate, description));


                        var featureWithDefaultStyle = new Feature { Geometry = sphericalMercatorCoordinate };
                        featureWithDefaultStyle.Styles.Add(new LabelStyle { Text = label });
                        memoryProvider.Features.Add(featureWithDefaultStyle);
                    }
                    catch (RuntimeBinderException)
                    {
                        Debug.WriteLine("No valid GPS coordinates found for this marker:" + label);
                    }
                    catch (JsonReaderException e)
                    {
                        // Console.WriteLine("Exception: " + e.Message);
                        //Console.WriteLine("Stack trace: " + e.StackTrace);
                        Debug.WriteLine("Data for " + label + " marker is invalid: " + e.Message);
                    }
                    catch (InvalidCastException)
                    {
                        Debug.WriteLine("GPS coordinates are in invalid format for this marker: " + label);
                    }

                }
            }

            return new MemoryLayer { Name = "Points with labels", DataSource = memoryProvider };
        }

        private static IStyle CreateColoredLabelStyle()
        {
            return new LabelStyle
            {
                Text = "Colors",
                BackColor = new Brush(Color.Blue),
                ForeColor = Color.White,
                Halo = new Pen(Color.Red, 4)
            };
        }
    }
}
