﻿using System;
using Xamarin.Forms;
using Newtonsoft.Json;
using System.Net.Http;
using System.Collections.Generic;
using MapsuiFormsSample.DataObjects;
using Microsoft.CSharp.RuntimeBinder;

namespace MapsuiFormsSample
{
    public partial class MainPage
    {
        HttpClient client = null;

        public MainPage()
        {
            // Required line when using XAML file.
            InitializeComponent();
            client = new HttpClient();
            client.BaseAddress = new Uri($"http://matrixrevolutions.ddns.net:8080/");
            ShowTestButton();
            ShowMarkersList();
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

            var json = await client.GetStringAsync($"demobicentennial/?q=mobileapi/node.json");
            dynamic markers = JsonConvert.DeserializeObject(json);
            List<Marker> markersList = new List<Marker>();
            foreach (dynamic marker in markers)
            {
                if ("historic_marker".Equals(marker.type.ToString()))
                {
                    markersList.Add(new Marker(marker.title.ToString(), marker.nid.ToString()));
                }
            }

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

            listView.ItemTapped += (sender, args) =>
            {
                Marker marker = args.Item as Marker;
                if (marker == null)
                {
                    return;
                }
                ShowMarkerLocation(marker.Title, "demobicentennial/?q=mobileapi/node/" + marker.NodeId);
                listView.SelectedItem = null;
            };

            ContentGrid.Children.Add(listView);

        }

        async void OnButtonClicked(object sender, EventArgs e)
        {
            // TODO: load the specific item clicked.
            ShowMarkerLocation("Test hardcoded marker", "demobicentennial/?q=mobileapi/node/2.json");
        }

        async void ShowMarkerLocation(string title, string url)
        {
            var json = await client.GetStringAsync(url);
            try
            {
                dynamic stuff = JsonConvert.DeserializeObject(json);
                double lat = stuff.field_coordinates.und[0].safe_value;
                double longitude = stuff.field_coordinates.und[1].safe_value;
                await Navigation.PushAsync(new MapPage(title, longitude, lat));
            }
            catch (RuntimeBinderException)
            {
                await DisplayAlert("Alert", "No valid GPS coordinates found for this marker", "OK");
            }
            catch (JsonReaderException e)
            {
                Console.WriteLine("Exception: " + e.Message);
                Console.WriteLine("Stack trace: " + e.StackTrace);
                await DisplayAlert("Alert", "Data for this marker is invalid: " + e.Message, "OK");


            }
            catch (InvalidCastException)
            {
                await DisplayAlert("Alert", "GPS coordinates are in invalid format for this marker", "OK");
            }
        }


    }
}
