using System ;
using System.Collections.Generic ;
using System.IO ;
using System.Linq ;
using System.Reflection ;
using System.Windows.Forms ;
using Gleed2D.Core ;
using StructureMap ;
using Keys = Microsoft.Xna.Framework.Input.Keys ;
using Level = Gleed2D.Core.Level ;

namespace GLEED2D
{
	public class InputHandlerForWhenEditorIdle : IHandleEditorInput
	{
		readonly IEditor _editor ;
		readonly IMainForm _mainForm ;
		ItemEditor _lastItem ;

		readonly Cursor _cursorDuplicate ;
		readonly IModel _model ;

		public InputHandlerForWhenEditorIdle( IEditor editor )
		{
			_editor = editor ;
			_mainForm = IoC.MainForm ;
			_model = IoC.Model ;

			Stream stream = safeGetManifestResourceStream( "GLEED2D.Resources.cursors.dragcopy.cur" ) ;

			_cursorDuplicate = new Cursor( stream ) ;
		}

		static Stream safeGetManifestResourceStream( string resourceName )
		{
			Stream stream = Assembly.GetExecutingAssembly( ).GetManifestResourceStream( resourceName ) ;

			if( stream == null )
			{
				throw new InvalidOperationException( @"Cannot load cursor named '{0}'.  Not found in the manifest resources." ) ;
			}

			return stream ;
		}

		public void Update( )
		{
			//get item under mouse cursor

			var model = ObjectFactory.GetInstance<IModel>( ) ;

			ItemEditor item = _editor.ItemUnderMouse ;

			bool controlButtonPressed = KeyboardStatus.IsKeyDown( Keys.LeftControl ) ;
			
			if( item != null )
			{
				_mainForm.SetToolStripStatusLabel1( item.ItemProperties.Name ) ;

				item.OnMouseOver( MouseStatus.WorldPosition ) ;

				if( controlButtonPressed )
				{
					_mainForm.SetCursorForCanvas( _cursorDuplicate ) ;
				}
			}
			else
			{
				_mainForm.SetToolStripStatusLabel1( string.Empty ) ;
			}

			if( item != _lastItem && _lastItem != null )
			{
				_lastItem.OnMouseOut( ) ;
			}

			_lastItem = item ;

			IEnumerable<ItemEditor> editors = selectedEditors( ).ToList(  ) ;

			if ( MouseStatus.IsNewLeftMouseButtonClick() || KeyboardStatus.IsNewKeyPress(Keys.D1))
			{
				if( item != null )
				{
					item.OnMouseButtonDown( MouseStatus.WorldPosition ) ;
				}

				if( controlButtonPressed && item != null )
				{
					_editor.StartCopyingSelectedItems( ) ;
				}
				else if( KeyboardStatus.IsKeyDown( Keys.LeftShift ) && item!=null )
				{
					model.ToggleSelectionOnItem( item ) ;
				}
				else if( editors.Contains( item ) )
				{
					_editor.StartMovingSelectedItems( ) ;
				}
				else if (!selectedEditors().Contains(item))
				{
					if( item != null )
					{
						_model.SelectEditor( item ) ;
						_editor.StartMovingSelectedItems(  );
					}
					else
					{
						_editor.CreateSelectionRectangle( ) ;
					}
				}
			}

			//MIDDLE MOUSE BUTTON CLICK
			bool anyEditorsSelected = editors.Any() ;

			if( MouseStatus.IsNewMiddleMouseButtonClick( ) || KeyboardStatus.IsNewKeyPress( Keys.D2 ) )
			{
				if( item != null )
				{
					item.OnMouseOut( ) ;
				}

				if( controlButtonPressed )
				{
					_editor.StartMovingCamera( ) ;
				}
				else
				{
					if( anyEditorsSelected  )
					{
						_editor.StartRotatingItems( ) ;
					}
				}
			}

			//RIGHT MOUSE BUTTON CLICK
			if( MouseStatus.IsNewRightMouseButtonClick( ) || KeyboardStatus.IsNewKeyPress( Keys.D3 ) )
			{
				if( item != null )
				{
					item.OnMouseOut( ) ;
				}

				if( anyEditorsSelected )
				{
					_editor.StartScalingSelectedItems( ) ;
				}
			}

			selectedEditors(  ).ForEach( e=>e.HandleKeyPressWhenFocused() );
		}

		IEnumerable<ItemEditor> selectedEditors( )
		{
			return getLevel( ).SelectedEditors ;
		}

		Level getLevel( )
		{
			return _model.Level ;
		}
	}
}