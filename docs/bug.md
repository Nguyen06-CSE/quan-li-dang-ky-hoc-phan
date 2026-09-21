# các chức năng đã tương tác được nhưng báo lỗi
- **Import**: tại thanh **meuControl** khi nhấn vào chương trình báo lỗi và out lập tức 
Chương trình báo lỗi như dưới
```
Unhandled exception. System.NullReferenceException: Object reference not set to an instance of an object.
   at QuanLyDKHP.App.ViewModels.MainWindowViewModel.NavigateTo(MenuItemViewModel menuItem) in /Users/caotiendattx/Developer/Do_An_Desktop/QLiDKHP/src/QuanLyDKHP.App/ViewModels/MainWindowViewModel.cs:line 168
   at CommunityToolkit.Mvvm.Input.RelayCommand`1.Execute(Object parameter)
   at Avalonia.Controls.MenuItem.OnClick(RoutedEventArgs e)
   at Avalonia.Controls.MenuItem.<>c.<.cctor>b__23_0(MenuItem x, RoutedEventArgs e)
   at Avalonia.Reactive.LightweightObservableBase`1.PublishNext(T value)
   at Avalonia.Interactivity.EventRoute.RaiseEventImpl(RoutedEventArgs e)
   at Avalonia.Interactivity.EventRoute.RaiseEvent(Interactive source, RoutedEventArgs e)
   at Avalonia.Interactivity.Interactive.RaiseEvent(RoutedEventArgs e)
   at Avalonia.Controls.MenuItem.Avalonia.Controls.IMenuItem.RaiseClick()
   at Avalonia.Controls.Platform.DefaultMenuInteractionHandler.Click(IMenuItem item)
   at Avalonia.Controls.Platform.DefaultMenuInteractionHandler.PointerReleased(Object sender, PointerReleasedEventArgs e)
   at Avalonia.Interactivity.EventRoute.RaiseEventImpl(RoutedEventArgs e)
   at Avalonia.Interactivity.EventRoute.RaiseEvent(Interactive source, RoutedEventArgs e)
   at Avalonia.Interactivity.Interactive.RaiseEvent(RoutedEventArgs e)
   at Avalonia.Input.MouseDevice.MouseUp(IMouseDevice device, UInt64 timestamp, IInputRoot root, Point p, PointerPointProperties props, KeyModifiers inputModifiers, IInputElement hitTest)
   at Avalonia.Input.MouseDevice.ProcessRawEvent(RawPointerEventArgs e)
   at Avalonia.Controls.PresentationSource.HandleInputCore(Object state)
   at Avalonia.Threading.Dispatcher.Send(SendOrPostCallback action, Object arg, Nullable`1 priority)
   at Avalonia.Native.TopLevelImpl.RawMouseEvent(AvnRawMouseEventType type, AvnPointerDeviceType deviceType, UInt64 timeStamp, AvnInputModifiers modifiers, AvnPoint point, AvnVector delta, Single pressure, Single xTilt, Single yTilt)
   at Avalonia.Native.Interop.Impl.__MicroComIAvnTopLevelEventsVTable.RawMouseEvent(Void* this, AvnRawMouseEventType type, AvnPointerDeviceType deviceType, UInt64 timeStamp, AvnInputModifiers modifiers, AvnPoint point, AvnVector delta, Single pressure, Single xTilt, Single yTilt)
--- End of stack trace from previous location ---
   at Avalonia.Native.DispatcherImpl.RunLoop(CancellationToken token)
   at Avalonia.Threading.DispatcherFrame.Run(IControlledDispatcherImpl impl)
   at Avalonia.Threading.Dispatcher.PushFrame(DispatcherFrame frame)
   at Avalonia.Threading.Dispatcher.MainLoop(CancellationToken cancellationToken)
   at Avalonia.Controls.ApplicationLifetimes.ClassicDesktopStyleApplicationLifetime.StartCore(String[] args)
   at Avalonia.Controls.ApplicationLifetimes.ClassicDesktopStyleApplicationLifetime.Start(String[] args)
   at Avalonia.ClassicDesktopStyleApplicationLifetimeExtensions.StartWithClassicDesktopLifetime(AppBuilder builder, String[] args, Action`1 lifetimeBuilder)
   at QuanLyDKHP.App.Program.Main(String[] args) in /Users/caotiendattx/Developer/Do_An_Desktop/QLiDKHP/src/QuanLyDKHP.App/Program.cs:line 12
   ```

# chức năng đã thiết kế UI/UX nhưng chưa hoạt động, chưa xây dựng function
- **các but tại Trang Chủ**
- **tại HK/LHP** cần kiểm tra chưa xác định chưa xây function

# chức năng cần chú ý nâng cấp bổ xung
- **danh sách đăng kí học phần đã có từ file import, cần chú ý cải thiện UI/UX để cán bộ thao tác nhanh**