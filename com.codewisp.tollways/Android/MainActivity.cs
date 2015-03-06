using System;
using System.Collections.Generic;
using Android.App;
using Android.Bluetooth;
using Android.Content;
using Android.Graphics;
using Android.Opengl;
using Android.OS;
using Android.Views;
using Android.Widget;
using EstimoteSdk;
using Java.Lang;
using Java.Util.Concurrent;
using ZXing;
using ZXing.QrCode;
using Region = EstimoteSdk.Region;
using Result = Android.App.Result;

namespace com.codewisp.tollways.android
{
    [Activity(Label = "App1", MainLauncher = true, Icon = "@drawable/icon")]
    public class MainActivity : Activity, BeaconManager.IServiceReadyCallback
    {
        private const int RequestEnableBt = 1234;

        private BeaconManager _beaconManager;
        private Region _region = new Region("DEMOREGION", "b9407f30-f5f8-466e-aff9-25556b57fe6d", null, null);

        private Button _entranceButton;
        private Button _useButton;
        private Button _exitButton;
        private TextView _credits;
        private ImageView _qrImage;

        private State _drivingState = State.OffTollway;
        private State DrivingState
        {
            get { return _drivingState; }
            set
            {
                _drivingState = value;
                StateChanged(DrivingState);
            }
        }

        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);

            // Set our view from the "main" layout resource
            SetContentView(Resource.Layout.Main);

            _beaconManager = new BeaconManager(this);
            _beaconManager.SetBackgroundScanPeriod(TimeUnit.Seconds.ToMillis(2), 0);

            _beaconManager.EnteredRegion += BeaconManagerOnEnteredRegion;
            _beaconManager.ExitedRegion += BeaconManagerOnExitedRegion;
        }

        protected override void OnResume()
        {
            base.OnResume();

            EnforceBluetooth();

            _qrImage = FindViewById<ImageView>(Resource.Id.QRImage);
            _qrImage.Visibility = ViewStates.Gone;

            _credits = FindViewById<TextView>(Resource.Id.Credits);
            _credits.Visibility = ViewStates.Gone;

            _entranceButton = FindViewById<Button>(Resource.Id.EntranceBtn);
            _useButton = FindViewById<Button>(Resource.Id.UseBtn);
            _exitButton = FindViewById<Button>(Resource.Id.ExitBtn);

            _entranceButton.Click += EntranceButtonOnClick;
            _useButton.Click += UseButtonOnClick;
            _exitButton.Click += ExitButtonOnClick;
        }

        private void EntranceButtonOnClick(object sender, EventArgs eventArgs)
        {
            if (DrivingState != State.OffTollway)
            {
                Toast.MakeText(this, "You need to be off the tollway to enter!", ToastLength.Short).Show();
                return;
            }

            DrivingState = State.OnEntrance;
        }
        private void UseButtonOnClick(object sender, EventArgs eventArgs)
        {
            if (DrivingState != State.OnEntrance)
            {
                Toast.MakeText(this, "You need to use the entrance to drive through the tollway!", ToastLength.Short).Show();
                return;
            }

            DrivingState = State.OnTollway;
        }

        private void ExitButtonOnClick(object sender, EventArgs eventArgs)
        {
            if (DrivingState != State.OnTollway)
            {
                Toast.MakeText(this, "You need to be using the tollway to exit", ToastLength.Short).Show();
                return;
            }

            DrivingState = State.OffTollway;
        }
        private void StateChanged(State drivingState)
        {
            switch (drivingState)
            {
                case State.OnEntrance:
                    EnterTollway();
                    break;

                case State.OnTollway:
                    DriveTollway();
                    break;

                default:
                    ExitTollway();
                    break;
            }
        }

        private void EnterTollway()
        {
            _qrImage.Visibility = ViewStates.Gone;

            _credits.Visibility = ViewStates.Visible;
            _credits.Text = "You have Php 345.00 left on your wallet.";
        }

        private void DriveTollway()
        {
            var writer = new BarcodeWriter
            {
                Options = new QrCodeEncodingOptions()
                {
                    Width = 400,
                    Height = 400,
                    Margin = 0
                },
                Format = BarcodeFormat.QR_CODE
            };

            var bitmap = writer.Write("http://www.codewisp.com/");
            _qrImage.Visibility = ViewStates.Visible;
            _qrImage.SetImageBitmap(bitmap);

            _credits.Visibility = ViewStates.Visible;
            _credits.Text = "Scan this QR code when you exit.";
        }

        private void ExitTollway()
        {
            _qrImage.Visibility = ViewStates.Visible;

            var alertDialog = new AlertDialog.Builder(this);
            alertDialog.SetMessage("Thank you for using the tollway. Pay Php 118.00 with your wallet?");
            alertDialog.SetNegativeButton("No", (sender, args) =>
            {
                _credits.Visibility = ViewStates.Visible;
                _credits.Text = "Please proceed to the counter to pay.";
            });

            alertDialog.SetPositiveButton("Yes", (sender, args) =>
            {
                _credits.Visibility = ViewStates.Visible;
                _credits.Text = "Thank you for using the tollway!";
            });

            alertDialog.SetTitle("Pay with your wallet?");
            alertDialog.Show();
        }

        private void BeaconManagerOnEnteredRegion(object sender, BeaconManager.EnteredRegionEventArgs enteredRegionEventArgs)
        {
            if (DrivingState == State.OffTollway)
            {
                DrivingState = State.OnEntrance;
                return;
            }

            if (DrivingState == State.OnTollway)
            {
                DrivingState = State.OffTollway;
                return;
            }

            // Whut
            Toast.MakeText(this, "Whut", ToastLength.Long).Show();
        }

        private void BeaconManagerOnExitedRegion(object sender, BeaconManager.ExitedRegionEventArgs exitedRegionEventArgs)
        {
            if (DrivingState == State.OnEntrance)
            {
                DrivingState = State.OnTollway;
                return;
            }

            if (DrivingState == State.OffTollway)
            {
                Toast.MakeText(this, "Bye!", ToastLength.Long).Show();

                _qrImage.Visibility = ViewStates.Gone;
                _credits.Visibility = ViewStates.Gone;
                return;
            }

            // Whut
            Toast.MakeText(this, "Whut", ToastLength.Long).Show();
        }

        # region Bluetooth connection
        private void EnforceBluetooth()
        {
            if (!_beaconManager.HasBluetooth)
            {
                Toast.MakeText(this, "Device does not have Bluetooth Low Energy", ToastLength.Long).Show();
                return;
            }

            if (!_beaconManager.IsBluetoothEnabled)
            {
                var enableBtIntent = new Intent(BluetoothAdapter.ActionRequestEnable);
                StartActivityForResult(enableBtIntent, RequestEnableBt);
            }
            else
            {
                ConnectToService();
            }
        }

        protected override void OnActivityResult(int requestCode, Result resultCode, Intent data)
        {
            if (requestCode == RequestEnableBt)
            {
                if (resultCode == Result.Ok)
                {
                    ConnectToService();
                }
                else
                {
                    Toast.MakeText(this, "Bluetooth not enabled", ToastLength.Long).Show();
                }
            }

            base.OnActivityResult(requestCode, resultCode, data);
        }

        private void ConnectToService()
        {
            _beaconManager.Connect(this);
        }
        public void OnServiceReady()
        {
            _beaconManager.StartMonitoring(_region);
        }
        #endregion

        protected override void OnDestroy()
        {
            _beaconManager.Disconnect();

            base.OnDestroy();
        }

        public enum State
        {
            OffTollway,
            OnTollway,
            OnEntrance
        }
    }
}

