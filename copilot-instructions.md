# Project details
F# 10
C# 14
.NET 10
Nullability checks enabled

# How to create custom controls

During the implementation of custom control, you should consider the following steps:
1. **Define the Control Class**: Create a new class that inherits from an appropriate base control class (e.g., `Control`, `UserControl`, etc.).
2. **Create .xaml file**: If your control has a visual representation, create a corresponding `.xaml` file to define the layout and appearance of the control in the same directory.
3. **Link the created .xaml file as a resource in the Generic.xaml file**
