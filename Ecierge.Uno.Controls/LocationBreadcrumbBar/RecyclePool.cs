#if !HAS_UNO
using IPanel = Ecierge.Uno.Controls.Breadcrumb.Interfaces.IPanel;
#endif

using System;
using System.Collections.Generic;
using System.Linq;
using Ecierge.Uno.Controls.Breadcrumb.Interfaces;
using Microsoft.UI.Xaml;

namespace Ecierge.Uno.Controls.LocationBreadcrumbBar
{
    public partial class RecyclePool
    {
        private readonly Dictionary<string, List<ElementInfo>> m_elements = new Dictionary<string, List<ElementInfo>>();

        private struct ElementInfo
        {
            public ElementInfo(UIElement element, IPanel owner)
            {
                Element = element;
                Owner = owner;
            }

            public UIElement Element { get; }
            public IPanel Owner { get; }
        };

        public void PutElement(UIElement element, string key)
            => PutElementCore(element, key, null /* owner */);

        public void PutElement(UIElement element, string key, UIElement owner)
            => PutElementCore(element, key, owner);

        public UIElement TryGetElement(string key)
            => TryGetElementCore(key, null /* owner */);

        public UIElement TryGetElement(string key, UIElement owner)
            => TryGetElementCore(key, owner);

        protected virtual void PutElementCore(UIElement element, string key, UIElement owner)
        {
            var winrtKey = key;
            var winrtOwner = owner;
            var winrtOwnerAsPanel = EnsureOwnerIsPanelOrNull(winrtOwner);

            ElementInfo elementInfo = new ElementInfo(element, winrtOwnerAsPanel);

            if (m_elements.TryGetValue(winrtKey, out var infos))
            {
                infos.Add(elementInfo);
            }
            else
            {
                var pool = new List<ElementInfo>();
                pool.Add(elementInfo);
                m_elements[winrtKey] = pool;
            }
        }

        protected virtual UIElement TryGetElementCore(string key, UIElement owner)
        {
            if (m_elements.TryGetValue(key, out var elements))
            {
                if (elements.Count > 0)
                {
                    ElementInfo elementInfo = default;

                    // Prefer an element from the same owner or with no owner so that we don't incur
                    // the enter/leave cost during recycling.
                    // TODO: prioritize elements with the same owner to those without an owner.
                    var winrtOwner = owner;
                    var iter = elements.FindIndex(elemInfo => elemInfo.Owner == winrtOwner || elemInfo.Owner == null);

                    if (iter < 0)
                    {
                        iter = elements.Count - 1;
                    }

                    elementInfo = elements[iter];
                    elements.RemoveAt(iter);

                    var ownerAsPanel = EnsureOwnerIsPanelOrNull(winrtOwner);
                    if (elementInfo.Owner != null && elementInfo.Owner != ownerAsPanel)
                    {
                        // Element is still under its parent. remove it from its parent.
                        var panel = elementInfo.Owner;
                        if (panel != null)
                        {
                            int childIndex = panel.Children.IndexOf(elementInfo.Element);
                            if (childIndex < 0)
                            {
                                throw new InvalidOperationException("ItemsRepeater's child not found in its Children collection.");
                            }

                            panel.Children.RemoveAt(childIndex);
                        }
                    }

                    return elementInfo.Element;
                }
            }

            return null;
        }

        // UNO: This has been customized as on WinUI the ItemsRecycler is actually a fake Panel
        private IPanel EnsureOwnerIsPanelOrNull(UIElement owner)
        {
            IPanel ownerAsPanel = null;
            if (owner != null)
            {
                ownerAsPanel = owner as IPanel;
                if (ownerAsPanel == null)
                {
                    throw new InvalidOperationException("owner must to be a Panel or null.");
                }
            }
            return ownerAsPanel;
        }

    }
}
