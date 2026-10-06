# SeismicExcel

`SeismicExcel` is an Excel-DNA add-in for Excel that exposes utility and test user-defined functions (UDFs) for geophysical analysis workflows. The project targets `.NET Framework 4.8` and currently includes library helpers, environment diagnostics, and validation functions for integration components such as `SharpProj`.



## Excel UDFs

> The UDFs are categorized using a naming convention consisting of three characters. **Avo** stands for amplitude versus offset,  **Lib** stands for library, etc.



### `avo - amplitude versus offset functions`

| Function                 | Description                                                  |
| ------------------------ | ------------------------------------------------------------ |
| `SE.avo.Shuey2`          | Shuey 2-parameter approximation to the Aki–Richards AVA equation. |
| `SE.avo.Shuey3`          | Shuey 3-parameter approximation to the Aki–Richards AVA equation. |
| `SE.avo.Bortfeld2`       | Bortfeld 2-term approximation to the Zoeppritz equations.    |
| `SE.avo.Bortfeld3`       | Bortfeld 3-term approximation to the Zoeppritz equations.    |
| `SE.avo.Fatti`           | Fatti 3-term approximation to the Zoeppritz equations.       |
| `SE.avo.AkiRichards`     | Aki-Richards approximation to the Zoeppritz equations.       |
| `SE.avo.Zoeppritz`       | Full Zoeppritz solution for reflection and transmission coefficients. |
| `SE.avo.GardnerDensity`  | Computes density from P-wave velocity using Gardner's equation. |
| `SE.avo.GardnerVelocity` | Computes P-wave velocity from density using the inverse Gardner equation. |
| `SE.avo.RayParameter`    | Computes ray parameter from incidence angle and P-wave velocity. |



### `fft - fourier transform functions`

| Function           | Description                                                  |
| ------------------ | ------------------------------------------------------------ |
| `SE.fft.FFT1D`     | Computes the forward FFT of a real or complex vector.        |
| `SE.fft.IFFT1D`    | Computes the inverse FFT of a real or complex vector.        |
| `SE.fft.Correlate` | Computes circular correlation of two real or complex sequences. |
| `SE.fft.Convolve`  | Computes circular convolution of two real or complex sequences. |



### `lib - library functions`

| Function | Description |
| --- | --- |
| `SE.lib.Version` | Returns the assembly version, file version, and informational version of the add-in. |
| `SE.lib.OperatingSystem` | Returns the current operating system and Excel version string reported by Excel. |



### `tpr - taper functions`

| Function | Description |
| --- | --- |
| `SE.tpr.Hamming1D` | Generates a full Hamming taper as a vertical spilled array. |
| `SE.tpr.Hanning1D` | Generates a full Hanning taper as a vertical spilled array. |
| `SE.tpr.Blackman1D` | Generates a full Blackman taper as a vertical spilled array. |
| `SE.tpr.Cosine1D` | Generates a full cosine taper as a vertical spilled array. |
| `SE.tpr.BlackmanHarris1D` | Generates a full Blackman-Harris taper as a vertical spilled array. |
| `SE.tpr.Bartlett1D` | Generates a full Bartlett taper as a vertical spilled array. |
| `SE.tpr.TukeyTails1D` | Generates a Tukey-style center lobe as a vertical spilled array. |
| `SE.tpr.TukeyCenter1D` | Generates a standard Tukey taper as a vertical spilled array. |



### `tst - test functions`

| Function                   | Description                                                  |
| -------------------------- | ------------------------------------------------------------ |
| `SE.tst.SayHello`          | Returns a simple greeting for the supplied name.             |
| `SE.tst.TestProj`          | Verifies that `SharpProj` initializes correctly and returns its version. |
| `SE.tst.TestDefaultValues` | Demonstrates use of Excel-friendly optional arguments by applying an optional scale and label. |



### `wav - wavelet functions`

| Function | Description |
| --- | --- |
| `SE.wav.Klauder` | Computes a Klauder wavelet for the supplied time samples. |
| `SE.wav.LinSweep` | Computes a linear sweep for the supplied time samples. |
| `SE.wav.LFSweep` | Computes a low-frequency sweep for the supplied time samples. |
| `SE.wav.LFSweepIF` | Computes instantaneous frequency for a low-frequency sweep. |
| `SE.wav.LFSweepIT` | Computes instantaneous time for a low-frequency sweep. |
| `SE.wav.Ormsby` | Computes an Ormsby wavelet for the supplied time samples. |
| `SE.wav.Ricker` | Computes a Ricker wavelet for the supplied time sample or samples. |
| `SE.wav.Sinc` | Computes a sinc wavelet for the supplied time samples. |



### `xfm` - coordinate transform functions

| Function | Description |
| --- | --- |
| `SE.xfm.LatLongToWebMercator` | Converts latitude and longitude in EPSG:4326 to Web Mercator meters in EPSG:3857 using `SharpProj`. |
| `SE.xfm.WebMercatorToLatLong` | Converts Web Mercator meters in EPSG:3857 to latitude and longitude in EPSG:4326 using `SharpProj`. |
| `SE.xfm.FromEpsgNoToEpsgNo` | Transforms X/Y coordinates from one EPSG CRS to another using `SharpProj`, with optional transform candidate selection. |
| `SE.xfm.ListTransformInfo` | Lists diagnostic information about the `SharpProj` transform selected between two EPSG CRSs. |
| `SE.xfm.ListTransformOptions` | Lists selectable transform candidates between two EPSG CRSs, including heuristic pros and cons. |



## Installation

To install the `SeismicExcel` add-in, follow these steps:

1. Download the latest release from the [Releases](https://github.com/yourusername/SeismicExcel/releases) page.
2. Extract the contents of the ZIP file to a directory of your choice.
3. Open Excel and navigate to `File` > `Options` > `Add-ins`.
4. At the bottom of the window, select `Excel Add-ins` from the `Manage` dropdown and click `Go...`.
5. Click `Browse...` and locate the `SeismicExcel.xll` file you extracted earlier.
6. Select the file and click `OK` to add it to your Excel environment.
7. Use of the various UDF's is demonstrated in the file SeismicExcelTests.xlsx



## Usage

Once installed, you can start using the functions directly in your Excel worksheets. Simply type the function name in a cell, followed by any required arguments. For example:

`=SE.lib.Version()`


This will return the version information of the `SeismicExcel` add-in.



## Contributing

We welcome contributions to the `SeismicExcel` project! If you would like to contribute, please follow these steps:

1. Fork the repository.
2. Create a new branch for your feature or bug fix.
3. Make your changes and commit them with clear messages.
4. Push your changes to your forked repository.
5. Submit a pull request detailing your changes.



## License

This project is licensed under the MIT License. See the [LICENSE](LICENSE.md) file for more details.



## Contact

For any inquiries or support, please reach out to the project maintainer at [Bart.Duijndam@ziggo.nl](mailto:Bart.Duijndam@ziggo.nl).