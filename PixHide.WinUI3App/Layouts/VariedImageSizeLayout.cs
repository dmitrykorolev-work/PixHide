// Modified from https://github.com/microsoft/WinUI-Gallery/blob/d1441382f46265a6606c9afbd2096391ba93bd5d/WinUIGallery/Layouts/VariedImageSizeLayout.cs

// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using Windows.Foundation;
using VirtualizingLayout = Microsoft.UI.Xaml.Controls.VirtualizingLayout;
using VirtualizingLayoutContext = Microsoft.UI.Xaml.Controls.VirtualizingLayoutContext;

namespace PixHide.WinUI3App.Layouts;

public partial class VariedImageSizeLayout : VirtualizingLayout
{
    public double Width { get; set; } = 150;

    protected override void OnItemsChangedCore(
        VirtualizingLayoutContext context,
        object source,
        NotifyCollectionChangedEventArgs args)
    {
        // Data source changed -> cached layout is no longer valid
        m_cachedBounds.Clear();
        m_cachedHeights.Clear();

        m_firstIndex = m_lastIndex = 0;
        m_lastAvailableWidth = 0.0;

        cachedBoundsInvalid = true;
        InvalidateMeasure();
    }

    protected override Size MeasureOverride(VirtualizingLayoutContext context, Size availableSize)
    {
        var viewport = context.RealizationRect;

        // Rebuild layout if width changed or explicitly invalidated
        if (availableSize.Width != m_lastAvailableWidth || cachedBoundsInvalid)
        {
            UpdateCachedBounds(availableSize);
            m_lastAvailableWidth = availableSize.Width;
        }

        // Ensure column offsets exist
        int numColumns = Math.Max(1, (int)(availableSize.Width / Width));
        if (m_columnOffsets.Count != numColumns)
        {
            m_columnOffsets.Clear();
            for (int i = 0; i < numColumns; i++)
            {
                m_columnOffsets.Add(0);
            }

            // Rebuild again because column count changed
            UpdateCachedBounds(availableSize);
        }

        m_firstIndex = GetStartIndex(viewport);

        int currentIndex = m_firstIndex;
        double nextOffset = -1.0;
        bool sizeChanged = false;

        // Measure visible items
        while (currentIndex < context.ItemCount && nextOffset < viewport.Bottom)
        {
            var child = context.GetOrCreateElementAt(currentIndex);
            child.Measure(new Size(Width, availableSize.Height));

            double measuredHeight = child.DesiredSize.Height;

            // Track height changes
            if (currentIndex >= m_cachedHeights.Count)
            {
                m_cachedHeights.Add(measuredHeight);
                sizeChanged = true;
            }
            else if (Math.Abs(m_cachedHeights[currentIndex] - measuredHeight) > 0.5)
            {
                m_cachedHeights[currentIndex] = measuredHeight;
                sizeChanged = true;
            }

            if (currentIndex >= m_cachedBounds.Count)
            {
                // New item -> assign temporary bounds
                int columnIndex = GetIndexOfLowestColumn(m_columnOffsets, out nextOffset);
                m_cachedBounds.Add(new Rect(columnIndex * Width, nextOffset, Width, measuredHeight));
                m_columnOffsets[columnIndex] += measuredHeight;
            }
            else
            {
                // Existing item -> use cached next offset
                if (currentIndex + 1 == m_cachedBounds.Count)
                {
                    GetIndexOfLowestColumn(m_columnOffsets, out nextOffset);
                }
                else
                {
                    nextOffset = m_cachedBounds[currentIndex + 1].Top;
                }
            }

            m_lastIndex = currentIndex;
            currentIndex++;
        }

        // If any size changed -> rebuild entire layout deterministically
        if (sizeChanged)
        {
            UpdateCachedBounds(availableSize);
        }

        return GetExtentSize(availableSize);
    }

    protected override Size ArrangeOverride(VirtualizingLayoutContext context, Size finalSize)
    {
        if (m_cachedBounds.Count > 0)
        {
            for (int index = m_firstIndex; index <= m_lastIndex && index < m_cachedBounds.Count; index++)
            {
                var child = context.GetOrCreateElementAt(index);
                child.Arrange(m_cachedBounds[index]);
            }
        }

        return finalSize;
    }

    private void UpdateCachedBounds(Size availableSize)
    {
        int numColumns = Math.Max(1, (int)(availableSize.Width / Width));

        // Reset column offsets
        m_columnOffsets.Clear();
        for (int i = 0; i < numColumns; i++)
        {
            m_columnOffsets.Add(0);
        }

        // Rebuild bounds deterministically from cached heights
        m_cachedBounds.Clear();

        for (int index = 0; index < m_cachedHeights.Count; index++)
        {
            double height = m_cachedHeights[index];

            int columnIndex = GetIndexOfLowestColumn(m_columnOffsets, out var nextOffset);
            m_cachedBounds.Add(new Rect(columnIndex * Width, nextOffset, Width, height));

            m_columnOffsets[columnIndex] += height;
        }

        cachedBoundsInvalid = false;
    }

    private int GetStartIndex(Rect viewport)
    {
        if (m_cachedBounds.Count == 0)
        {
            return 0;
        }

        // Find first item intersecting viewport
        for (int i = 0; i < m_cachedBounds.Count; i++)
        {
            var currentBounds = m_cachedBounds[i];
            if (currentBounds.Y < viewport.Bottom &&
                currentBounds.Bottom > viewport.Top)
            {
                return i;
            }
        }

        return 0;
    }

    private int GetIndexOfLowestColumn(List<double> columnOffsets, out double lowestOffset)
    {
        int lowestIndex = 0;
        lowestOffset = columnOffsets[lowestIndex];

        for (int index = 1; index < columnOffsets.Count; index++)
        {
            var currentOffset = columnOffsets[index];
            if (lowestOffset > currentOffset)
            {
                lowestOffset = currentOffset;
                lowestIndex = index;
            }
        }

        return lowestIndex;
    }

    private Size GetExtentSize(Size availableSize)
    {
        if (m_columnOffsets.Count == 0)
        {
            return new Size(availableSize.Width, 0);
        }

        double largestColumnOffset = m_columnOffsets[0];
        for (int index = 1; index < m_columnOffsets.Count; index++)
        {
            largestColumnOffset = Math.Max(largestColumnOffset, m_columnOffsets[index]);
        }

        return new Size(availableSize.Width, largestColumnOffset);
    }

    int m_firstIndex = 0;
    int m_lastIndex = 0;
    double m_lastAvailableWidth = 0.0;

    List<double> m_columnOffsets = new List<double>();
    List<Rect> m_cachedBounds = new List<Rect>();
    List<double> m_cachedHeights = new List<double>();

    private bool cachedBoundsInvalid = false;

    public void Refresh()
    {
        // Do NOT clear caches -> preserve deterministic layout
        cachedBoundsInvalid = true;
        InvalidateMeasure();
    }
}