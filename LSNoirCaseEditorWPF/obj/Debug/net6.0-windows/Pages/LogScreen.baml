   M S B A M L   `   `   `  ����  LSNoirCaseEditorWPF)    #LSNoirCaseEditorWPF.Pages.LogScreen   5      W SPresentationCore, Version=6.0.2.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35\ XPresentationFramework, Version=6.0.2.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35e aSystem.Windows.Controls.Ribbon, Version=6.0.2.0, Culture=neutral, PublicKeyToken=b77a5c561934e089R NWindowsBase, Version=6.0.2.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35F 9http://schemas.microsoft.com/winfx/2006/xaml/presentation     5      R NSystem.Xaml, Version=6.0.2.0, Culture=neutral, PublicKeyToken=b77a5c561934e089:x,http://schemas.microsoft.com/winfx/2006/xaml     5      @d2http://schemas.microsoft.com/expression/blend/2008     5      Jmc;http://schemas.openxmlformats.org/markup-compatibility/2006     5      D @ModernWpf, Version=0.9.6.0, Culture=neutral, PublicKeyToken=nullM IModernWpf.Controls, Version=0.9.6.0, Culture=neutral, PublicKeyToken=null2controls!http://schemas.modernwpf.com/2019   5      .��5	      �   � RowDefinitions  5
   
   �� 5      $
��Auto �6   �� 5      $��* �6    � ColumnDefinitions 5   
   �� 5      $
��Auto �6   �� 5      $
��Auto �6   �� 5      $
��Auto �6   .+�5   
   �� $��0��6   $��0��6!   $��Activity Log Viewer��61   $��36(�6L   $��10,10q�6Z   �� 5   
   -   5         ��Name$ 
debugCheck��5      $��0��6'   $��1��64   $��Enable Debug Log��6D    d� 	IsChecked$
 TrueY�6_   $��10,10,10,10q�6p    d� IsThreeState$ False��6�   6�   (  "ModernWpf.Controls.HyperlinkButton  5   
   -   5          Name$ logLink��5   #   $��0��64   $��3��6A   $��12(�6Q   $��10,10q�6_   6n   �� 5   
   -   5         ��Name$ 
ScrollView��5   H   $��1��6   $��0��6$   $��3��64   $
��Auto=�6\   $��Stretch=�6   $��Visible=�6�   $��Stretch=�6�   .��5      �� -   5        $��LogBox��5      $��Stretch=�6&   $��Stretch=�6B   $
��1200��6`   $
��Wrap=�6m   5      5      5      