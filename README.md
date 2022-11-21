# SDRSharp.HiDPI

`SDR#` is a good program for working with SDR receivers. This plugin resizes a number of `SDR#` elements so that you can use the program in HiDPI mode.
Personally, I stopped updating the `SDR#` and use the latest classic noskin version `v1.0.0.1716`, because after switching to Telerik libraries, the program became impossible to use in HiDPI GUI mode. It feels like `v1.0.0.1716` is Windows XP, and everything after that is like `Windows Vista`.

In any case, we will pay tribute to the author and note that all rights to `SDR#` belong to Youssef Touil. Thank him for his work.


## Installation

1. Copy the SDRSharp.HiDPI.dll into SDR# installation directory
2. Add the following line in the sharpPlugins section of PlugIns.xml file (this should be the last key):

	&lt;add key="HiDPI" value="SDRSharp.HiDPI.HiDPIPlugin,SDRSharp.HiDPI" /&gt;

3. Launch SDR# and cross fingers :)


## Changelog

<code>
&#x2713; 0.0.1: Initial MVP for 200% HiDPI scaling
</code>


## Screenshots

<!--img src="Screenshots/image_non_HiDPI.png" width="1280" /-->
<!--img src="Screenshots/image_HiDPI.png" width="1280" /-->


## Bugs? Ideas?

Please report them using the issues on the Github project!
