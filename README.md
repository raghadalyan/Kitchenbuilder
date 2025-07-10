Smart Kitchen Builder – SolidWorks Automation Project
This project automates the creation of kitchen layouts using the SolidWorks API and .NET 8. It enables users to enter kitchen dimensions via a UI dashboard and generates up to three optimized kitchen designs. Each layout includes suggested base arrangements, cabinet options, and appliance placements. The system programmatically modifies SolidWorks .SLDPRT files based on user input.

Features
Parametric Design – Adjusts base and cabinet sizes according to kitchen dimensions.

Three Layout Options – Automatically generates up to 3 kitchen designs per input.

Interactive UI – Built with Blazor/Maui to collect user data and control generation flow.

SolidWorks Integration – Connects to SolidWorks using its API to modify and save part files.

Customizable Cabinets – Allows setting drawer count and cabinet height for each base.

Appliance Placement – Logic-based sink and cooktop positioning under windows, in center, or side-by-side.

Getting Started
Prerequisites
✅ SolidWorks
Must be installed locally with API access enabled.

✅ .NET SDK (8.0+)
Download from Microsoft .NET 8.0

✅ SolidWorks API DLLs
You will need the following:

SolidWorks.Interop.sldworks.dll

SolidWorks.Interop.swconst.dll

TODO: Place these DLLs inside a libs/ folder.
🧭 Workflow
Launch Dashboard – The user opens the Smart Kitchen Builder dashboard UI.

Input Floor Dimensions – User enters kitchen width and length.

Wall & Window Setup – User defines number of walls, their lengths, and window positions.

Room Preview – The system visualizes the kitchen room based on the inputs.

Choose Floor Material / Color – User selects material and color for the kitchen floor.

Generate Base Layout Options – The system suggests up to 3 layout options for the kitchen base.

Select Base Layout – User selects one of the suggested base layouts.

Choose Base Material / Color – User selects material and color for the base.

Suggest Countertop – The system recommends a suitable countertop based on the chosen base.

Customize Countertop (Optional) – If the user wants to customize, they can modify the countertop and choose its material and color.

Suggest Sink & Cooktop Layout – The system proposes possible placements for the sink and cooktop (centered, under window, side-by-side).

User Chooses Sink/Cooktop Option – User selects one of the proposed layouts.

Suggest Island (Optional) – If the layout allows, the system suggests adding a kitchen island.

Choose Island Material / Color – User selects material and color for the island.

Create Cabinets – Cabinets are automatically generated along the visible bases.

Place Appliances – System suggests and positions built-in appliances such as microwave and oven.

Save Final Design – All parts and images are saved to the output folder, including .SLDPRT files and rendered view screenshots.

Key Files

LayoutLauncher.cs: Splits SolidWorks and dashboard view
JsonReader.cs: Loads user input and applies it in SolidWorks
EditSketchDim_IModel.cs: Edits a specific SolidWorks sketch dimension by name and value
Show_Bodies_In_Sld_IModel.cs: Makes specific SolidWorks bodies visible by name
Hide_Bodies_In_Sld_IModel.cs: Hides specific SolidWorks bodies by name
SaveImgs.cs: Saves multiple standard view images (top, front, right, isometric) of the model to a folder
