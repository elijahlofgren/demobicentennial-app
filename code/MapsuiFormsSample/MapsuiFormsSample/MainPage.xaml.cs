﻿using System;
using Xamarin.Forms;
using System.Collections.Generic;
using MapsuiFormsSample.DataObjects;
using MapsuiFormsSample.Services;
using System.Diagnostics;
using System.Threading.Tasks;
using Plugin.Permissions.Abstractions;
using Plugin.Permissions;
#if __MOBILE__
using Plugin.Geolocator;
using Plugin.Geolocator.Abstractions;
#endif

namespace MapsuiFormsSample
{
    public partial class MainPage : ILocationServiceChangeWatcher
    {
        //HttpClient client = null;
        private IMarkerService _markerService;
        private ILocationService _locationService;

        public MainPage()
        {
            _markerService = new MarkerService();
            _locationService = new LocationService(this);

            // Required line when using XAML file.
            InitializeComponent();
            /*
            client = new HttpClient();
            client.BaseAddress = new Uri($"http://13.82.106.207/");
            */
            //ShowTestButton();
            ShowMarkersList();

            _locationService.InitLocationChangeListener();
        }

       


        void ShowTestButton()
        {
            Label testLabel = new Label
            {
                Text = "Test Label",
                HorizontalOptions = LayoutOptions.Center
            };
            Button button = new Button
            {
                Text = "Click Me!",
                BorderWidth = 1,
                HorizontalOptions = LayoutOptions.Center,
                VerticalOptions = LayoutOptions.CenterAndExpand
            };
            button.Clicked += OnButtonClicked;
            ContentGrid.Children.Add(testLabel);
            ContentGrid.Children.Add(button);
        }

        async void ShowMarkersList()
        {
            List<Marker> markersList = await _markerService.GetAllMarkers();

            // Create the ListView.
            ListView listView = new ListView
            {
                // Source of data items.
                ItemsSource = markersList,

                // Define template for displaying each item.
                // (Argument of DataTemplate constructor is called for 
                //      each item; it must return a Cell derivative.)
                ItemTemplate = new DataTemplate(() =>
                {
                    // Create views with bindings for displaying each property.
                    Label nameLabel = new Label();
                    nameLabel.SetBinding(Label.TextProperty, "Title");

                    Label nodeIdLabel = new Label();
                    nodeIdLabel.SetBinding(Label.TextProperty,
                        new Binding("NodeId", BindingMode.OneWay,
                            null, null, "Node Id:  {0:d}"));


                    // Return an assembled ViewCell.
                    return new ViewCell
                    {
                        View = new StackLayout
                        {
                            Padding = new Thickness(0, 5),
                            Orientation = StackOrientation.Horizontal,
                            Children =
                                {
                                    new StackLayout
                                    {
                                        VerticalOptions = LayoutOptions.Center,
                                        Spacing = 0,
                                        Children =
                                        {
                                            nameLabel,
                                            nodeIdLabel
                                        }
                                        }
                                }
                        }
                    };
                })
            };

            listView.ItemTapped += async (sender, args) => 
            {
                Marker marker = args.Item as Marker;
                if (marker == null)
                {
                    return;
                }
                // TODO: Show marker detail screen
                //ShowMarkerLocation(marker.Title, "/?q=mobileapi/node/" + marker.NodeId);

                await Navigation.PushAsync(new MarkerInfoPage(marker));
                listView.SelectedItem = null;
            };

            ContentGrid.Children.Add(listView);

        }

        async void OnButtonClicked(object sender, EventArgs e)
        {
            // TODO: load the specific item clicked.
            // ShowMarkerLocation("Test hardcoded marker", "/?q=mobileapi/node/2.json");
        }

        public async void PositionChanged(object sender, PositionEventArgs e)
        {
            Debug.WriteLine("PositionChanged called");
            //If updating the UI, ensure you invoke on main thread
            var position = e.Position;
            var output = "PositionChanged() called. Full: Lat: " + position.Latitude + " Long: " + position.Longitude;
            output += "\n" + $"Time: {position.Timestamp}";
            output += "\n" + $"Heading: {position.Heading}";
            output += "\n" + $"Speed: {position.Speed}";
            output += "\n" + $"Accuracy: {position.Accuracy}";
            output += "\n" + $"Altitude: {position.Altitude}";
            output += "\n" + $"Altitude Accuracy: {position.AltitudeAccuracy}";
            Debug.WriteLine(output);
            /*if (_initialLoadCompleted)
            {
                // Redraw map

                _mapControl.NativeMap.Layers.Remove(_layer);
                _layer = await GenerateLayer(position);

                _mapControl.NativeMap.Layers.Add(_layer);
            }
            else
            {
                Debug.WriteLine("Initial load not completed so skipping map redraw");
            }
            */
        }

        /*
        async void ShowMarkerLocation(string title, string url)
        {
            var json = await client.GetStringAsync(url);
            try
            {
                dynamic stuff = JsonConvert.DeserializeObject(json);
                double lat = stuff.field_coordinates.und[0].safe_value;
                double longitude = stuff.field_coordinates.und[1].safe_value;
                // TODO: Open Google maps URL.
                //await Navigation.PushAsync(new MapPage(title, longitude, lat));
            }
            catch (RuntimeBinderException)
            {
                await DisplayAlert("Alert", "No valid GPS coordinates found for this marker", "OK");
            }
            catch (JsonReaderException e)
            {
                // Console.WriteLine("Exception: " + e.Message);
                //Console.WriteLine("Stack trace: " + e.StackTrace);
                await DisplayAlert("Alert", "Data for this marker is invalid: " + e.Message, "OK");


            }
            catch (InvalidCastException)
            {
                await DisplayAlert("Alert", "GPS coordinates are in invalid format for this marker", "OK");
            }
        }
        */


    }
}
