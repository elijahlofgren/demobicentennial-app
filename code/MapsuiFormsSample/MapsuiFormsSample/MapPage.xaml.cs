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
using MapsuiFormsSample.Services;
#if __MOBILE__
using Plugin.Geolocator;
using Plugin.Geolocator.Abstractions;
#endif

namespace MapsuiFormsSample
{
    public partial class MapPage
    {
        private IMarkerService _markerService;
        private MapsUIView _mapControl = null;
        private List<Marker> _markersList = new List<Marker>();

        public MapPage()
        {
            _markerService = new MarkerService();
            GenerateMap();

        }

        public async void GenerateMap()
        {
            this.Title = "Map";
            InitializeComponent();

            _mapControl = new MapsUIView();
            _mapControl.NativeMap.Layers.Add(OpenStreetMap.CreateTileLayer());

            Position userPosition = null;
#if __MOBILE__
            if (IsLocationAvailable())
            {
                userPosition = await GetCurrentLocation();
            }
            else
            {
                RequestLocationPermission();
            }
#endif

            ILayer layer = await CreateLayerAsync(userPosition);
            _mapControl.NativeMap.Layers.Add(layer);

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
                        await DisplayAlert("Need location", "Location needed to allow you to play this game", "OK");
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


        /* Unmerged change from project 'MapsuiFormsSample.UWP'
        Before:
                public ILayer CreateLayer(Position userPosition)
        After:
                public ILayer CreateLayerAsync(Position userPosition)
        */

        /* Unmerged change from project 'MapsuiFormsSample.iOS'
        Before:
                public ILayer CreateLayer(Position userPosition)
        After:
                public ILayer CreateLayerAsync(Position userPosition)
        */

        /* Unmerged change from project 'MapsuiFormsSample'
        Before:
                public ILayer CreateLayer(Position userPosition)
        After:
                public ILayer CreateLayerAsync(Position userPosition)
        */
        public async Task<ILayer> CreateLayerAsync(Position userPosition)
        {
            var memoryProvider = new MemoryProvider();
            ShowUserCurrentLocation(memoryProvider, userPosition);
            //string markersJson = TestMarkerData.JsonTestData;
            _markersList = await _markerService.GetAllMarkers();
            foreach (Marker marker in _markersList)
            {
                var featureWithDefaultStyle = new Feature { Geometry = 
                    marker.LocationSphericalMercator };
                featureWithDefaultStyle.Styles.Add(new LabelStyle { Text = marker.Title });
                memoryProvider.Features.Add(featureWithDefaultStyle);



            }

            return new MemoryLayer { Name = "Points with labels", DataSource = memoryProvider };
        }


        private void ShowUserCurrentLocation(MemoryProvider memoryProvider, Position userPosition)
        {
            if (userPosition != null)
            {
                Point sphericalMercatorCoordinate = SphericalMercator.FromLonLat(userPosition.Longitude, userPosition.Latitude);
                string description = "Your Position";
                // TODO: Eventually put on another layer.
                _markersList.Add(new Marker("Your Position", "-1", sphericalMercatorCoordinate, description));


                var featureWithDefaultStyle = new Feature { Geometry = sphericalMercatorCoordinate };
                featureWithDefaultStyle.Styles.Add(new LabelStyle { Text = description });
                memoryProvider.Features.Add(featureWithDefaultStyle);
            }
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
