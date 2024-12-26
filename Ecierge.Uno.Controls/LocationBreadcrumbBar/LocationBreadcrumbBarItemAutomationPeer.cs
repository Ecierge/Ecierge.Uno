// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference BreadcrumbBarItemAutomationPeer.cpp, tag winui3/release/1.5.3, commit 2a60e27

#nullable enable

using Microsoft/* UWP don't rename */.UI.Xaml.Controls;
#if HAS_UNO
using Uno.UI.Helpers.WinUI;
using Uno.UI.Helpers.Xaml;
#endif
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Automation.Provider;

namespace Ecierge.Uno.Controls.LocationBreadcrumbBar;

/// <summary>
/// Exposes BreadcrumbBar types to Microsoft UI Automation.
/// </summary>
public partial class LocationBreadcrumbBarItemAutomationPeer : FrameworkElementAutomationPeer, IInvokeProvider
{
    /// <summary>
    /// Initializes a new instance of the BreadcrumbBarItemAutomationPeer class.
    /// </summary>
    /// <param name="owner"></param>
    public LocationBreadcrumbBarItemAutomationPeer(LocationBreadcrumbBarItem owner) : base(owner)
    {
    }

    //IAutomationPeerOverrides
    /*protected override string GetLocalizedControlTypeCore() =>                                                          
    ResourceAccessor.GetLocalizedStringResource(*
        ResourceAccessor.SR_BreadcrumbBarItemLocalizedControlType);*/

    protected override object GetPatternCore(PatternInterface patternInterface)
    {
        if (patternInterface == PatternInterface.Invoke)
        {
            return this;
        }

        return base.GetPatternCore(patternInterface);
    }

    protected override string GetClassNameCore() => nameof(LocationBreadcrumbBarItem);

    protected override AutomationControlType GetAutomationControlTypeCore() =>
        AutomationControlType.Button;

    private LocationBreadcrumbBarItem? GetImpl()
    {
        LocationBreadcrumbBarItem? impl = null;

        if (Owner is LocationBreadcrumbBarItem locationBreadcrumbItem)
        {
            impl = locationBreadcrumbItem;
        }

        return impl;
    }

    /// <summary>
    /// Sends a request to invoke the item associated with the automation peer.
    /// </summary>
    public void Invoke()
    {
        if (GetImpl() is { } locationBreadcrumbItem)
        {
            locationBreadcrumbItem.OnClickEvent(null, null);
        }
    }
}
